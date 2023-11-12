using UnityEngine;
using System.Collections;
using System.Linq;
using System.Collections.Generic;
using System;
using Random = UnityEngine.Random;
using Unity.VisualScripting;

public class MainGameController : Singleton<MainGameController> {
	
	//***************************************************************************//
	// This class is the main controller of the game and manages customer creation,
	// time management, money management, game state and win/lose state.
	// it also manages available seats in your shop.
	//***************************************************************************//

	// freeplay goal ballance
	public int freeplayGoalBallance = 10000;
	
												 
	//******************//
	// Mission Variables (for Career mode) //
	public int availableTime; 				//Seconds
	//******************//
	// Common variables
	public bool  canUseCandy {get;set;}
	//******************//

	
	//******************//
	 public string gameMode{get;set;}			//game mode setting (freeplay or career)
	 public int gameTime{get;set;}
	 public int requiredBalance{get;set;}
	 public int globalTrashLoss {get;set;}= 15;	//when player trashes a product or ingredient, we deduct a fixed loss from player balance.
	//*******************

	//public game objects
	public GameObject[] customers;	//list of all available customers (different patience and textures)

	//public variables
	//*****
	//How many seats are available in this shop?
	//These two array should always be the same size (Length)
	public bool[] availableSeatForCustomers;
	public Vector3[] seatPositions;
	public GameObject[] additionalItems;	//Items that can be purchased via in-game shop.
	public GameObject endGamePlane;			//main endgame plane
	public GameObject endGameStatus;		//gameobject which shows the texture of win/lose states
	public Texture2D[] endGameTextures;		//textures for win/lose states
	public GameObject nextButton;			//next button loads the next available level when player beats a level
	//*****

	 public bool  deliveryQueueIsFull{get;set;}	//delivery queue can accept 6 ingredients. more is not acceptable.
	 public int deliveryQueueItems{get;set;}				//number of items in delivery queue
	 public List<int> deliveryQueueItemsContent {get;set;}= new List<int>();	//conents of delivery queue

	///game timer vars
	private string remainingTime;
	private int seconds;
	private int minutes;
	
	private int delay;					//delay between creating a new customer (smaller number leads to faster customer creation)
	private bool  canCreateNewCustomer;	//flag to prevent double calls to functions

	//Money and GameState
	public int totalMoneyMade{get;set;}
	//private int totalMoneyLost;
	public bool  gameIsFinished{get;set;}	//Flag

	///////////////////////////////////////
	public int slotState {get;set;}		= 0;		//available slots for product creation (same as delivery queue)
	public int maxSlotState{get;set;}			//maximum available slots in delivery queue (set in init)

	//****************************
	// 3D Text Objects 
	//****************************
	public GameObject moneyText;
	public GameObject missionText;
	public GameObject timeText;

	//AudioClips
	public AudioClip timeEndSfx;
	public AudioClip winSfx;
	public AudioClip loseSfx;


	public List<CustomerController> customersInScene {private set;get;}= new List<CustomerController>();

	protected override void Awake (){
		base.Awake();
		Init();
	}


	//***************************************************************************//
	// Init everything here.
	//***************************************************************************//
	void Init (){

		Application.targetFrameRate = 50; //Optional based on the target platform
		slotState = 0;
		maxSlotState = 6;
		
		deliveryQueueIsFull = false;
		deliveryQueueItems = 0;
		deliveryQueueItemsContent.Clear();
		
		//customersAppeared = 0;
		//satisfiedCutomer = 0;
		//angryCustomer = 0;
		totalMoneyMade = 0;
		//totalMoneyLost = 0;
		gameIsFinished = false;

		nextButton.SetActive (false);	//only shows when we finish a level in career mode with success.
		
		seconds = 0;
		minutes = 0;
		
		//delay = 12; //Optimal value should be between 5 (Always crowded) and 15 (Not so crowded) seconds. 
		delay = 11;
		canCreateNewCustomer = false;
		
		//set all seats as available at the start of the game. No seat is taken yet.
		for(int i = 0; i < availableSeatForCustomers.Length; i++) {
			availableSeatForCustomers[i] = true;
		}
		
		//check if player previously purchased these items..
		//ShopItem index starts from 1.
		for(int j = 0; j < additionalItems.Length; j++) {
			//format the correct string we use to store purchased items into playerprefs
			string shopItemName = "shopItem-" + (j+1).ToString();;
			if(PlayerPrefs.GetInt(shopItemName) == 1) {
				//we already purchased this item
				additionalItems[j].SetActive(true);
			} else {
				additionalItems[j].SetActive(false);
			}
		}
		
		//check game mode.
		if(PlayerPrefs.HasKey("gameMode"))
			gameMode = PlayerPrefs.GetString("gameMode");
		else
			gameMode = "FREEPLAY"; //default game mode
			
		switch(gameMode) {
			case "FREEPLAY":
				requiredBalance = freeplayGoalBallance;
				gameTime = 0;
				canUseCandy = true;
				break;
			case "CAREER":
				requiredBalance = PlayerPrefs.GetInt("careerGoalBallance");
				availableTime = PlayerPrefs.GetInt("careerAvailableTime");
				//check if we are allowed to use candy in this career level
				canUseCandy = (PlayerPrefs.GetInt("canUseCandy") == 1) ? true : false;
				break;
		}
	}


	//***************************************************************************//
	// Starting delay. Optional.
	//***************************************************************************//
	IEnumerator Start (){
		yield return new WaitForSeconds(2);
		canCreateNewCustomer = true;
	}


	//***************************************************************************//
	// FSM
	//***************************************************************************//
	void Update (){

			//no more ingredient can be picked
			if(deliveryQueueItems >= maxSlotState)
				deliveryQueueIsFull = true;
			else	
				deliveryQueueIsFull = false;
			
			if(!gameIsFinished) {
				manageClock();
				manageGuiTexts();
				StartCoroutine(checkGameWinState());
			}
			
			//create a new customer if there is free seat and game is not finished yet
			if(canCreateNewCustomer && !gameIsFinished) {
				if(monitorAvailableSeats() != 0) {
					createCustomer( freeSeatIndex[Random.Range(0, freeSeatIndex.Count)] );
				} else {
					//print("No free seat is available!");
				}
			}
		}
		

	//***************************************************************************//
	// New Customer creation routine.
	//***************************************************************************//
	void createCustomer ( int _seatIndex  ){

		//set flag to prevent double calls 
		canCreateNewCustomer = false;
		StartCoroutine(reactiveCustomerCreation());
		
		//which customer?
		var customerPicked = customers[Random.Range(0, customers.Length)];
		
		//which seat
		Vector3 seat = seatPositions[_seatIndex];
		//mark the seat as taken
		
		
		//create customer
		int offset = -11;
		GameObject newCustomer = Instantiate(customerPicked, new Vector3(offset, 0.8f, 0.2f), Quaternion.Euler(0, 180, 0)) as GameObject;
		var customerController = newCustomer.GetComponent<CustomerController>();
		//any post creation special Attributes?
		customerController.mySeat = _seatIndex;
		//set customer's destination
		customerController.destination = seat;

		availableSeatForCustomers[customerController.mySeat] = false;
		customersInScene.Add(customerController);
	}

	public void OnCustomerLeave(CustomerController customer)
	{
		availableSeatForCustomers[customer.mySeat] = true;
		customersInScene.Remove(customer);
	}


	//***************************************************************************//
	// customer creation is active again
	//***************************************************************************//
	IEnumerator reactiveCustomerCreation (){
		yield return new WaitForSeconds(delay);
		canCreateNewCustomer = true;
		yield break;
	}


	//***************************************************************************//
	// check if there is any free seat for customers and if true, return their index(s)
	//***************************************************************************//
	private List<int> freeSeatIndex = new List<int>();
	int monitorAvailableSeats (){
		freeSeatIndex = new List<int>();
		for(int i = 0; i < availableSeatForCustomers.Length; i++) {
			if(availableSeatForCustomers[i] == true)
				freeSeatIndex.Add(i);
		}
		
		//debug
		//print("Available seats: " + freeSeatIndex);
		
		if(freeSeatIndex.Count > 0)
			return -1;
		else
			return 0;
	}


	//***************************************************************************//
	// GUI text management
	//***************************************************************************//
	void manageGuiTexts (){
		moneyText.GetComponent<TextMesh>().text = "$" + totalMoneyMade.ToString();
		missionText.GetComponent<TextMesh>().text = "$" + requiredBalance.ToString();
	}

		
	//***************************************************************************//
	// Game clock manager
	//***************************************************************************//
	void manageClock (){

		if(gameIsFinished)
			return;
		
		if(gameMode == "FREEPLAY") {

			gameTime = (int)Time.timeSinceLevelLoad;
			seconds = Mathf.CeilToInt(Time.timeSinceLevelLoad) % 60;
			minutes = Mathf.CeilToInt(Time.timeSinceLevelLoad) / 60; 
			remainingTime = string.Format("{0:00} : {1:00}", minutes, seconds); 
			timeText.GetComponent<TextMesh>().text = remainingTime.ToString();
			
		} else if(gameMode == "CAREER") {
		
			gameTime = (int)(availableTime - Time.timeSinceLevelLoad);
			seconds = Mathf.CeilToInt(availableTime - Time.timeSinceLevelLoad) % 60;
			minutes = Mathf.CeilToInt(availableTime - Time.timeSinceLevelLoad) / 60; 
			remainingTime = string.Format("{0:00} : {1:00}", minutes, seconds); 
			timeText.GetComponent<TextMesh>().text = remainingTime.ToString();
		}

		/*
		if(seconds == 0 && minutes == 0) {
			gameIsFinished = true;
			processGameFinish();
		}
		*/
	}


	

	//***************************************************************************//
	// Game Win/Lose State
	//***************************************************************************//
	IEnumerator checkGameWinState (){
		
		if(gameIsFinished)
			yield break;

		if(gameMode == "CAREER" && gameTime <= 0 && totalMoneyMade < requiredBalance) {
		
			print("Time is up! You have failed :(");	//debug the result
			gameIsFinished = true;						//announce the new status to other classes
			endGamePlane.SetActive(true);				//show the endGame plane
			endGameStatus.GetComponent<Renderer>().material.mainTexture = endGameTextures[1];	//show the correct texture for result
			playNormalSfx(timeEndSfx);
			yield return new WaitForSeconds(2.0f);
			playNormalSfx(loseSfx);
			
		} else if(gameMode == "CAREER" && gameTime > 0 && totalMoneyMade >= requiredBalance) {
			
			//save career progress
			saveCareerProgress();
			
			//grant the prize
			int levelPrize = PlayerPrefs.GetInt("careerPrize");
			int currentMoney = PlayerPrefs.GetInt("PlayerMoney");
			currentMoney += levelPrize;
			PlayerPrefs.SetInt("PlayerMoney", currentMoney);
			
			print("Wow, You beat the level! :)");
			gameIsFinished = true;
			endGamePlane.SetActive(true);
			endGameStatus.GetComponent<Renderer>().material.mainTexture = endGameTextures[0];
			playNormalSfx(winSfx);

			//save gametime for level stars system
			PlayerPrefs.SetFloat("Level-" + PlayerPrefs.GetInt("careerLevelID").ToString() , Time.timeSinceLevelLoad);

			//show next level button
			nextButton.SetActive (true);
			
		} else if(gameMode == "FREEPLAY" && totalMoneyMade >= requiredBalance) {
		
			print("Wow, You beat the goal in freeplay mode. But You can continue... :)");
			playNormalSfx(winSfx);
			//gameIsFinished = true; 
			//we can still play in freeplay mode. 
			//there is no end here unless user stops the game and choose exit.
		}
	}

	
	//********************************************************
	// Save user progress in career mode.
	//********************************************************
	void saveCareerProgress (){
		int currentLevelID = PlayerPrefs.GetInt("careerLevelID");
		int userLevelAdvance = PlayerPrefs.GetInt("userLevelAdvance");
		
		//if this is the first time we are beating this level...
		if(userLevelAdvance < currentLevelID) {
			userLevelAdvance++;
			PlayerPrefs.SetInt("userLevelAdvance", userLevelAdvance);
		}
	}


	///***********************************************************************
	/// play normal audio clip
	///***********************************************************************
	void playNormalSfx ( AudioClip _sfx  ){
		GetComponent<AudioSource>().clip = _sfx;
		if(!GetComponent<AudioSource>().isPlaying)
			GetComponent<AudioSource>().Play();
	}
}