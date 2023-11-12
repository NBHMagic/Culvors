using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Net;

public class CustomerController : MonoBehaviour
{
    public CustomerData customerData;
    public CustomerReferences customerReferences;
    public float customerPatience => customerData.customerPatience;

    // Audio Clips
    public AudioClip orderIsOkSfx => customerReferences.orderIsOkSfx;
    public AudioClip orderIsNotOkSfx => customerReferences.orderIsNotOkSfx;
    public AudioClip receivedSomethingGoodSfx => customerReferences.receivedSomethingGoodSfx;

    //*** Customer Moods ***//
    //We use different materials for each mood
    /* Currently we have 4 moods: 
	 [0]Defalut
	 [1]Bored 
	 [2]Satisfied
	 [3]Angry
	 we change to appropriate material whenever needed. 
	*/
    public Material[] customerMoods => customerReferences.customerMoods;
    private int moodIndex;

    //**** Special Variables ******//
    // Warning!!! - Do not Modify //
    public int mySeat;                          //Do not modify this.
    public Vector3 destination;
    
    public bool isCloseEnoughToDelivery;        //Do not modify this.
    public bool isCloseEnoughToCandy;           //Do not modify this. 
    public bool isCloseEnoughToSideRequest;     //Do not modify this.
                                                //****************************//


    //Private customer variables
    private string customerName;                //random name

    private float currentCustomerPatience;      //current patience of the customer
  
    public float customerPatientDecreaseRate { get;set;}

    //Patience bar GUI items and vars
    public GameObject patienceBarFG;
    public GameObject patienceBarBG;
    private bool patienceBarSliderFlag;
    internal float leaveTime;
    private float creationTime;

    public GameObject money3dText;              //3d text mesh over customers head after successful delivery

    //Transforms
    private Vector3 startingPosition;           //reference

    //all about more than 1 plate goes here:
    private GameObject[] serverPlates;          //all available plates inside the game
    private float[] distanceToPlates;           //distance to all available plates
    private GameObject target;                  //which plate is nearest? that will be the main target

    public CustomerRequestController customerRequestController{get;private set;}
    public Renderer customerRenderer {get;private set;}

    public State state = State.Init;
    public enum State
    {
        Init,
        MovingToSeat,
        Waiting,
        Leaving,
        Flying
    }
    void Awake()
    {

        target = null;
        serverPlates = GameObject.FindGameObjectsWithTag("serverPlate");
        distanceToPlates = new float[serverPlates.Length];

        
        patienceBarFG.SetActive(false);
        patienceBarBG.SetActive(false);


        isCloseEnoughToDelivery = false;
        isCloseEnoughToCandy = false;
        isCloseEnoughToSideRequest = false;


        currentCustomerPatience = customerPatience;
        moodIndex = 0;
        leaveTime = 0;

        creationTime = Time.time;
        startingPosition = transform.position;
        customerRequestController = GetComponent<CustomerRequestController>();
        customerRenderer = GetComponentInChildren<MeshRenderer>();

    }


    void Start()
    {
        Init();
        StartCoroutine(goToSeat());
    }
    //***************************************************************************//
    // Here we will initialize all customer related variables.
    //***************************************************************************//

    private GameObject[] helperIngredients;

    void Init()
    {
        //we give this customer a nice name
        customerName = "Customer_" + Random.Range(100, 10000);
        gameObject.name = customerName;

        
        customerRequestController.PickCustomerRequest();
    }


    //***************************************************************************//
    // After this customer has been instantiated by MainGameController,
    // it starts somewhere outside game scene and then go to it's position (seat)
    // with a nice animation. Then asks for it's order.
    //***************************************************************************//
    private float timeVariance;
    IEnumerator goToSeat()
    {
        state = State.MovingToSeat;

        timeVariance = Random.value;
        
        while (true)
        {
            transform.position = new Vector3(transform.position.x + (Time.deltaTime * customerData.customerMoveSpeed),
                                             startingPosition.y - 0.25f + (Mathf.Sin((Time.time + timeVariance) * 10) / 8),
                                             transform.position.z);

            if (transform.position.x >= destination.x)
            {
                patienceBarSliderFlag = true; //start the patience bar
                customerRequestController.ReachedSeat();
                patienceBarFG.SetActive(true);
                patienceBarBG.SetActive(true);
                state = State.Waiting;
                break;
            }
            yield return 0;
        }
    }


    ///**********************************************************
    /// Takes an array and return the lowest number in it, 
    /// with the optional index of this lowest number (position of it in the array)
    ///**********************************************************
    Vector2 findMinInArray(float[] _array)
    {
        int lowestIndex = -1;
        float minval = 1000000.0f;
        for (int i = 0; i < _array.Length; i++)
        {
            if (_array[i] < minval)
            {
                minval = _array[i];
                lowestIndex = i;
            }
        }
        //return the Vector2(minimum population, index of this minVal in the argument Array)
        return (new Vector2(minval, lowestIndex));
    }


    //***************************************************************************//
    // FSM
    //***************************************************************************//
    void Update()
    {
        for (int i = 0; i < serverPlates.Length; i++)
        {
            distanceToPlates[i] = Vector3.Distance(serverPlates[i].transform.position, gameObject.transform.position);
            //find the correct (nearest plate) target
            
            target = serverPlates[(int)findMinInArray(distanceToPlates).y];
            //print (gameObject.name + " target plate is: " + target.name);
        }

        if (patienceBarSliderFlag)
            StartCoroutine(patienceBar());

        //Manage customer's mood by changing it's material
        updateCustomerMood();
    }


    //***************************************************************************//
    // We need these events to be checked at the eend of every frame.
    //***************************************************************************//
    void LateUpdate()
    {

        //check if this customer is close enough to delivery, in order to receive it.
        if (target)
        {
            if (target.GetComponent<PlateController>().isReadyToServe)
                checkDistanceToDelivery();
        }

        //check if this customer is close enough to delivery, in order to receive it.
        //if(PlateController.canDeliverOrder && gameObject.tag == "customer") {
        //	checkDistanceToDelivery();
        //}

        //check if this customer is close enough to a candy, in order to receive it.
        if (CandyController.canDeliverCandy && gameObject.tag == "customer")
        {
            checkDistanceToCandy();
        }

        //check if this customer is close enough to a side-request delivery, in order to receive it.
        //we must also check if this customer wants a side-request or not!
        if (SideRequestsController.canDeliverSideRequest && customerRequestController.customerSideReq != -1 && gameObject.tag == "customer")
        {
            checkDistanceToSideRequest();
        }

        //check the status of customers with both main order and side-request, and look if they already received both their orders.
        if (customerRequestController.IsAllRequestsFulfilled && state == State.Waiting)
        {
            settle();
        }
    }


    //***************************************************************************//
    // make the customer react to events by changing it's material (and texture)
    //***************************************************************************//
    void updateCustomerMood()
    {
        //if customer has waited for half of his/her patience, make him/her bored.
        if (state == State.Waiting)
        {
            if (currentCustomerPatience <= customerPatience / 2)
                moodIndex = 1;
            else
                moodIndex = 0;
        }

        customerRenderer.material = customerMoods[moodIndex];
    }


    //***************************************************************************//
    // fill customer's patience bar and make it full again.
    //***************************************************************************//
    void fillCustomerPatience()
    {
        currentCustomerPatience = customerPatience;
        patienceBarFG.transform.localScale = new Vector3(1,
                                                         patienceBarFG.transform.localScale.y,
                                                         patienceBarFG.transform.localScale.z);
        patienceBarFG.transform.localPosition = new Vector3(0,
                                                            patienceBarFG.transform.localPosition.y,
                                                            patienceBarFG.transform.localPosition.z);
    }


    //***************************************************************************//
    // check distance to delivery
    // we check if player is dragging the order towards the customer or towards it's
    // requestBubble. the if the order was close enough to any of these objects,
    // we se the deliver flag to true.
    //***************************************************************************//
    private float distanceToDelivery;
    private float frameDistanceToDelivery;
    void checkDistanceToDelivery()
    {
        distanceToDelivery = Vector3.Distance(transform.position, target.transform.position);
        frameDistanceToDelivery = Vector3.Distance(customerRequestController. requestBubble.transform.position, target.transform.position);
        //print(gameObject.name + " distance to candy is: " + distanceToDelivery + ".");

        //Hardcoded integer for distance.
        if (distanceToDelivery < 1.0f || frameDistanceToDelivery < 1.0f)
        {
            isCloseEnoughToDelivery = true;
            //tint color
            customerRenderer.material.color = new Color(0.5f, 0.5f, 0.5f);
        }
        else
        {
            isCloseEnoughToDelivery = false;
            //reset tint color
            customerRenderer.material.color = new Color(1, 1, 1);
        }
    }


    //***************************************************************************//
    // check distance to Candy
    //***************************************************************************//
    private float distanceToCandy;
    private GameObject deliveryCandy;
    void checkDistanceToCandy()
    {
        //first find the deliveryCandy GameObject
        if (!deliveryCandy)
        { //do we already found a candy?
            deliveryCandy = GameObject.FindGameObjectWithTag("deliveryCandy");
        }

        distanceToCandy = Vector3.Distance(transform.position, deliveryCandy.transform.position);
        //print(gameObject.name + " distance to candy is: " + distanceToCandy + ".");

        //Hardcoded integer for distance.
        if (distanceToCandy < 2.0f)
        {
            isCloseEnoughToCandy = true;
            //tint color
            customerRenderer.material.color = new Color(0.5f, 0.5f, 0.5f);
        }
        else
        {
            isCloseEnoughToCandy = false;
            //reset tint color
            customerRenderer.material.color = new Color(1, 1, 1);
        }
    }


    //***************************************************************************//
    // check distance to side-request
    //***************************************************************************//
    private float distanceToSideRequest;
    private float frameDistanceToSideRequest;
    private GameObject deliverySideRequest;
    void checkDistanceToSideRequest()
    {
        //first find the delivery SideRequest GameObject
        if (!deliverySideRequest)
        { //do we already found a candy?
            deliverySideRequest = GameObject.FindGameObjectWithTag("deliverySideRequest");
        }

        distanceToSideRequest = Vector3.Distance(transform.position, deliverySideRequest.transform.position);
        frameDistanceToSideRequest = Vector3.Distance(customerRequestController.requestBubble.transform.position, deliverySideRequest.transform.position);
        //print(gameObject.name + " distance to SideRequest is: " + distanceToSideRequest + ".");

        //Hardcoded integer for distance.
        if (distanceToSideRequest < 2.0f || frameDistanceToSideRequest < 2.0f)
        {
            isCloseEnoughToSideRequest = true;
            //tint color
            customerRenderer.material.color = new Color(0.5f, 0.5f, 0.5f);
        }
        else
        {
            isCloseEnoughToSideRequest = false;
            //reset tint color
            customerRenderer.material.color = new Color(1, 1, 1);
        }
    }


    //***************************************************************************//
    // show and animate progress bar based on customer's patience
    //***************************************************************************//
    IEnumerator patienceBar()
    {
        patienceBarSliderFlag = false;
        while (currentCustomerPatience >  0 && customerPatientDecreaseRate > 0)
        {
            currentCustomerPatience -= Time.deltaTime * customerPatientDecreaseRate;
            patienceBarFG.transform.localScale = new Vector3(patienceBarFG.transform.localScale.x - ((0.02f / customerPatience) * Time.deltaTime * Application.targetFrameRate),
                                                             patienceBarFG.transform.localScale.y,
                                                             patienceBarFG.transform.localScale.z);
            patienceBarFG.transform.position = new Vector3(patienceBarFG.transform.position.x,
                                                           patienceBarFG.transform.position.y - ((0.021f / customerPatience) * Time.deltaTime * Application.targetFrameRate),
                                                           patienceBarFG.transform.position.z);
            yield return 0;
        }
        if (currentCustomerPatience <= 0)
        {
            patienceBarFG.GetComponent<Renderer>().enabled = false;
            //customer is angry and will leave with no food received.
            StartCoroutine(leave());
        }

    }


    //***************************************************************************//
    // receive and check order contents
    //***************************************************************************//
    public void ReceiveMainDish(List<int> _getOrder)
    {
        var response = customerRequestController.DeliverMainDishRequest(_getOrder);
        ResponseToRequestAnswer(response);
      
       
    }
    //***************************************************************************//
    // receive a side-request
    //***************************************************************************//
    public void receiveSideRequest(int _ID)
    {
        var response = customerRequestController.DeliverSideRequest(_ID);
        ResponseToRequestAnswer(response);
    }

    void ResponseToRequestAnswer(CustomerRequestController.CustomerRequestResponseType responseType)
    {
        switch(responseType)
        {
            case CustomerRequestController.CustomerRequestResponseType.Correct:
                orderIsCorrect();
                break;
            case CustomerRequestController.CustomerRequestResponseType.Leave:
                OrderIsIncorrectAndLeave();
                break;
            case CustomerRequestController.CustomerRequestResponseType.Nothing:
                break;
        }
    }



    //***************************************************************************//
    // if order is delivered correctly
    //***************************************************************************//
    void orderIsCorrect()
    {
        print("Order is correct.");

        //if the customer just asked for an order with NO sideRequest, we make him settle and leave instantly.
        if (customerRequestController.IsAllRequestsFulfilled)
        {
            settle();
        }
        else
        { //But if it also wants a side-request, we enter another loop to check if the second wish is fulfilled or not.
          //first hide the main order
            customerRequestController.UpdateVisual();
            foreach (var helper in helperIngredients)
                helper.GetComponent<Renderer>().enabled = false;
    
            playSfx(receivedSomethingGoodSfx);
        }
    }


    //***************************************************************************//
    // if order is NOT delivered correctly
    //***************************************************************************//
    void OrderIsIncorrectAndLeave()
    {
        print("Order is not correct.");
        moodIndex = 3;
        playSfx(orderIsNotOkSfx);
        StartCoroutine(leave());
    }


    //***************************************************************************//
    // receive a candy to refill the patience bar :)
    //***************************************************************************//
    public void receiveCandy()
    {
        //Candy Received!
        print(gameObject.name + " received a candy!");
        playSfx(receivedSomethingGoodSfx);
        //fill customer's patient
        fillCustomerPatience();
    }


    

    //***************************************************************************//
    // Customer should pay and leave the restaurant.
    //***************************************************************************//
    void settle()
    {
        moodIndex = 2;  //make him/her happy :)

        //give cash, money, bonus, etc, here.
        float leaveTime = Time.time;
        int remainedPatienceBonus = (int)Mathf.Round(customerPatience - (leaveTime - creationTime));

        //if we have purchased additional items for our restaurant, we should receive more tips
        int tips = 0;
        if (PlayerPrefs.GetInt("shopItem-1") == 1) tips += 2;   //if we have seats
        if (PlayerPrefs.GetInt("shopItem-2") == 1) tips += 3;   //if we have music player
        if (PlayerPrefs.GetInt("shopItem-3") == 1) tips += 5;   //if we have flowers

        
        //Prior to update 1.8.3
        /*int finalMoney = availableProducts[customerNeeds].GetComponent<ProductManager>().price +
                            remainedPatienceBonus +
                            tips;*/

        int finalMoney =    customerRequestController.CalculateRequestMoney() +
                            remainedPatienceBonus +
                            tips;

        MainGameController.Instance.totalMoneyMade += finalMoney;
        GameObject money3d = Instantiate(money3dText,
                                            transform.position + new Vector3(0, 0, -0.8f),
                                            Quaternion.Euler(0, 0, 0)) as GameObject;
        print("FinalMoney: " + finalMoney);
        money3d.GetComponent<TextMeshController>().myText = "$" + finalMoney.ToString();

        playSfx(orderIsOkSfx);
        StartCoroutine(leave());
    }


    //***************************************************************************//
    // Leave routine with get/set ers and animations.
    //***************************************************************************//
    public IEnumerator leave()
    {

        //prevent double animation
        if (state != State.Waiting)
            yield break;
        state = State.Leaving;

        //set the leave flag to prevent multiple calls to this function
   
        //we need to change the tag of this customer, in order to not be able to receive new deliveries.
        gameObject.tag = "Untagged";

        //animate (close) patienceBar
        StartCoroutine(animate(Time.time, patienceBarBG, 0.7f, 0.8f));
        yield return new WaitForSeconds(0.3f);

        //animate (close) request bubble
        StartCoroutine(animate(Time.time, customerRequestController. requestBubble, 0.75f, 0.95f));
        yield return new WaitForSeconds(0.4f);

        //animate mainObject (hide it, then destroy it)
        while (transform.position.x < 10)
        {
            transform.position = new Vector3(transform.position.x + (Time.deltaTime * customerData.customerMoveSpeed),
                                             startingPosition.y - 0.25f + (Mathf.Sin(Time.time * 10) / 8),
                                             transform.position.z);

            if (transform.position.x >= 10)
            {
                MainGameController.Instance.OnCustomerLeave(this);
                Destroy(gameObject);
                yield break;
            }
            yield return 0;
        }
    }


    //***************************************************************************//
    // animate customer.
    //***************************************************************************//
    IEnumerator animate(float _time, GameObject _go, float _in, float _out)
    {
        float t = 0.0f;
        while (t <= 1.0f)
        {
            t += Time.deltaTime * 10;
            _go.transform.localScale = new Vector3(Mathf.SmoothStep(_in, _out, t),
                                                   _go.transform.localScale.y,
                                                   _go.transform.localScale.z);
            yield return 0;
        }
        float r = 0.0f;
        if (_go.transform.localScale.x >= _out)
        {
            while (r <= 1.0f)
            {
                r += Time.deltaTime * 2;
                _go.transform.localScale = new Vector3(Mathf.SmoothStep(_out, 0.01f, r),
                                                       _go.transform.localScale.y,
                                                       _go.transform.localScale.z);
                if (_go.transform.localScale.x <= 0.01f)
                    _go.SetActive(false);
                yield return 0;
            }
        }
    }


    //***************************************************************************//
    // Play AudioClips
    //***************************************************************************//
    void playSfx(AudioClip _sfx)
    {
        GetComponent<AudioSource>().clip = _sfx;
        if (!GetComponent<AudioSource>().isPlaying)
            GetComponent<AudioSource>().Play();
    }

}