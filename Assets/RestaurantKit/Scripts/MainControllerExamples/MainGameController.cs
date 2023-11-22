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
	
	//*******************

	
	public GameObject[] additionalItems;	//Items that can be purchased via in-game shop.
	public GameObject endGamePlane;			//main endgame plane
	public GameObject endGameStatus;		//gameobject which shows the texture of win/lose states
	public Texture2D[] endGameTextures;		//textures for win/lose states
	public GameObject nextButton;			//next button loads the next available level when player beats a level
	//*****

	 public bool  deliveryQueueIsFull{get;set;}	//delivery queue can accept 6 ingredients. more is not acceptable.
	 public int deliveryQueueItems{get;set;}				//number of items in delivery queue
	 public List<int> deliveryQueueItemsContent {get;set;}= new List<int>();	//conents of delivery queue

	
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
		
		gameIsFinished = false;

		nextButton.SetActive (false);	//only shows when we finish a level in career mode with success.

	
		
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
				
				gameTime = 0;
				canUseCandy = true;
				break;
			case "CAREER":
				
				availableTime = PlayerPrefs.GetInt("careerAvailableTime");
				//check if we are allowed to use candy in this career level
				canUseCandy = (PlayerPrefs.GetInt("canUseCandy") == 1) ? true : false;
				break;
		}
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

	}
		




	//***************************************************************************//
	// GUI text management
	//***************************************************************************//
	void manageGuiTexts (){
		moneyText.GetComponent<TextMesh>().text = "$" + RestaurantManager.Instance.totalCoinEarned.ToString();
		missionText.GetComponent<TextMesh>().text = "$" + RestaurantManager.Instance.coinEarnTarget.ToString();
	}

		
	//***************************************************************************//
	// Game clock manager
	//***************************************************************************//
	void manageClock (){

		if(gameIsFinished)
			return;
		
		if(gameMode == "FREEPLAY") {

			gameTime = (int)Time.timeSinceLevelLoad;
			var seconds = Mathf.CeilToInt(Time.timeSinceLevelLoad) % 60;
			var minutes = Mathf.CeilToInt(Time.timeSinceLevelLoad) / 60; 
			var remainingTime = string.Format("{0:00} : {1:00}", minutes, seconds); 
			timeText.GetComponent<TextMesh>().text = remainingTime.ToString();
			
		} else if(gameMode == "CAREER") {
		
			gameTime = (int)(availableTime - Time.timeSinceLevelLoad);
			var seconds = Mathf.CeilToInt(availableTime - Time.timeSinceLevelLoad) % 60;
			var minutes = Mathf.CeilToInt(availableTime - Time.timeSinceLevelLoad) / 60; 
			var remainingTime = string.Format("{0:00} : {1:00}", minutes, seconds); 
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

		if(gameMode == "CAREER" && gameTime <= 0 && !RestaurantManager.Instance.coinEarnTargetReached) {
		
			print("Time is up! You have failed :(");	//debug the result
			gameIsFinished = true;						//announce the new status to other classes
			endGamePlane.SetActive(true);				//show the endGame plane
			endGameStatus.GetComponent<Renderer>().material.mainTexture = endGameTextures[1];	//show the correct texture for result
			playNormalSfx(timeEndSfx);
			yield return new WaitForSeconds(2.0f);
			playNormalSfx(loseSfx);
			
		} else if(gameMode == "CAREER" && gameTime > 0 && RestaurantManager.Instance.coinEarnTargetReached) {
			
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
			
		} else if(gameMode == "FREEPLAY" && RestaurantManager.Instance.coinEarnTargetReached) {
		
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