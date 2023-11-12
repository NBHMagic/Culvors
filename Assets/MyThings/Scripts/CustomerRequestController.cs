using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CustomerRequestController : MonoBehaviour
{
    CustomerData customerData => customerController.customerData;
    public DictionaryIntFloat customerRequestProbability => customerData.customerRequestProbability;
    public GameObject[] availableMainDishes => customerData.availableMainDishes;
    public GameObject[] availableSides => customerData.availableSides;


    public GameObject requestBubble;            //reference to (gameObject) bubble over customers head
    public GameObject mainDishPositionMarker;   

    public Text messageText;

    private GameObject mainDishGO;
    private GameObject sideGO;

    CustomerController customerController;
  

    public int customerMainDishReq {get;private set;}
    public int customerSideReq {get;private set;}

    public int customerMessageReq {get;private set;}
    private CustomerRequestType pickedRequestType;
    private CustomerRequestType fulfilledRequestType;

    
    private int[] productIngredientsIDs => availableMainDishes[customerMainDishReq].GetComponent<ProductManager>().ingredientsIDs;

    [System.Flags]
    public enum CustomerRequestType
    {
        Nothing = 0,
        MainDish = 1,
        Side = 2,
        Message = 4,
    }

    public enum CustomerRequestResponseType
    {
        Correct,
        Leave,
        Nothing,
    }

    void Awake()
    {
        customerController = GetComponentInParent<CustomerController>();
        requestBubble.SetActive(false);
    }

    public void PickCustomerRequest()
    {
        pickedRequestType = (CustomerRequestType)RandomUtil.GetRandomElementWeighted(customerRequestProbability);
        customerController.customerPatientDecreaseRate = 30 * 0.02f;
        if(pickedRequestType.IsATypeof(CustomerRequestType.MainDish))
         {
            PickMainDishReq();
        }
         if(pickedRequestType.IsATypeof(CustomerRequestType.Side))
         {
            PickSideReq();
         }
         if(pickedRequestType.IsATypeof(CustomerRequestType.Message))
         {
            PickMessageReq();
         }

         GenerateVisual();
    
    }

    void PickMainDishReq()
    {
        //choose a product for this customer
        // if (customerMainDishReq == 0)
        {
            //for freeplay mode, customers can choose any product. But in career mode,
            //they have to choose from products we allowed in CareerLevelSetup class.
            if (PlayerPrefs.GetString("gameMode") == "CAREER")
            {
                int totalAvailableProducts = PlayerPrefs.GetInt("availableProducts");
                customerMainDishReq = PlayerPrefs.GetInt("careerProduct_" + Random.Range(0, totalAvailableProducts).ToString());
                customerMainDishReq -= 1; //Important. We count the indexes from zero, while selecting the products from 1.
                                    //so we subtract a unit from customerNeeds to be equal to main AvailableProducts array.
            }
            else
            {
                customerMainDishReq = Random.Range(0, availableMainDishes.Length);
            }
        }

    }

    void PickSideReq()
    {
        customerSideReq = Random.Range(0, availableSides.Length);
    }
    
    void PickMessageReq()
    {
        customerMessageReq = Random.Range(0, customerData.messages.Length);
        customerController.customerPatientDecreaseRate = 0;
    }
    void GenerateVisual()
    {
        if (pickedRequestType.IsATypeof(CustomerRequestType.MainDish))
        {
            //get and show product's image
            mainDishGO = Instantiate(availableMainDishes[customerMainDishReq], mainDishPositionMarker.transform.position, Quaternion.Euler(90, 180, 0)) as GameObject;
            mainDishGO.name = "customerNeeds";
            mainDishGO.transform.localScale = new Vector3(0.18f, 0.1f, 0.13f);
            mainDishGO.transform.parent = requestBubble.transform;

        }
        
        //if customer wants a side-request, show it
        if (pickedRequestType.IsATypeof(CustomerRequestType.Side))
        {
            sideGO = Instantiate(availableSides[customerSideReq], mainDishPositionMarker.transform.position + new Vector3(0, -0.65f, 0), Quaternion.Euler(0, 180, 0)) as GameObject;

            sideGO.transform.localScale = new Vector3(0.7f, 0.7f, 0.001f);
            sideGO.transform.parent = requestBubble.transform;
            if (pickedRequestType.IsATypeof(CustomerRequestType.MainDish))
            {
                    //move the main order a little up! to make some space for the side-request
                mainDishGO.transform.position = new Vector3(mainDishGO.transform.position.x,
                                                          mainDishGO.transform.position.y + 0.4f,
                                                          mainDishGO.transform.position.z);
                
            }
            else
            {
                sideGO.transform.position = mainDishPositionMarker.transform.position;
            }
            //if this side-req item needs to be processed, show it (ask it) with processed image :)
            if (sideGO.GetComponent<SideRequestMover>().beforeAfterMat.Length > 1)
                sideGO.GetComponent<SideRequestMover>().setMaterial(sideGO.GetComponent<SideRequestMover>().beforeAfterMat[1]);

            sideGO.GetComponent<SideRequestMover>().enabled = false;
        }

        if(pickedRequestType.IsATypeof(CustomerRequestType.Message))
        {
            messageText.text = customerData.messages[customerMessageReq];
        }

        // //show ingredients in the bubble if needed by developer (can make the game easy or hard)
        // helperIngredients = new GameObject[productIngredients];
        // for (int j = 0; j < productIngredients; j++)
        // {
        //     int ingredientID = productIngredientsIDs[j] - 1; // Array always starts from 0, while we named our ingredients from 1.
        //     helperIngredients[j] = Instantiate(availableIngredients[ingredientID], helperIngredientDummies[j].transform.position, Quaternion.Euler(90, 180, 0)) as GameObject;
        //     helperIngredients[j].tag = "ingIcon";
        //     helperIngredients[j].name = "helperIngredient #" + j;
        //     helperIngredients[j].GetComponent<ProductMover>().enabled = false;
        //     helperIngredients[j].transform.parent = requestBubble.transform;

        //     if (!showProductIngredientHelpers)
        //     {
        //         helperIngredients[j].GetComponent<Renderer>().enabled = false;
        //     }
        // }
    }

    public CustomerRequestResponseType DeliverMainDishRequest(List<int> deliveredOrder)
    {
        if(pickedRequestType.IsATypeof(CustomerRequestType.Message))
            return CustomerRequestResponseType.Nothing;
        if(!pickedRequestType.IsATypeof(CustomerRequestType.MainDish))
            return CustomerRequestResponseType.Leave;
        //print("Order received. contents are: " + _getOrder);

        //check the received order with the original one (customer's wish).
        int[] myOriginalOrder = productIngredientsIDs;
        List<int> myReceivedOrder = deliveredOrder;

        //check if the two array are the same, meaning that we received what we were looking for.
        //print(myOriginalOrder + " - " + myReceivedOrder);

        //1.check the length of two arrays
        if (myOriginalOrder.Length == myReceivedOrder.Count)
        {
            //2.compare two arrays
            bool detectInequality = false;
            for (int i = 0; i < myOriginalOrder.Length; i++)
            {
                if (myOriginalOrder[i] != myReceivedOrder[i])
                {
                    detectInequality = true;
                }
            }

            if (!detectInequality)
            {
                fulfilledRequestType |= CustomerRequestType.MainDish;
                return CustomerRequestResponseType.Correct;
            }

        }
        
        
        return CustomerRequestResponseType.Leave;
    }

    public CustomerRequestResponseType DeliverSideRequest(int id)
    {
         if(pickedRequestType.IsATypeof(CustomerRequestType.Message))
            return CustomerRequestResponseType.Nothing;
        if(!pickedRequestType.IsATypeof(CustomerRequestType.Side))
            return CustomerRequestResponseType.Leave;
        //side-request Received!
        print(gameObject.name + " received a side-request, ID: " + (id - 1) + " - Original request ID was: " + customerSideReq);
        //check if this is what we want
        if (customerSideReq == id - 1)
        { //we subtract 1 from _ID, because we start side-request items from 0, while Ids starts from 1.
          //we received the right side-request, so the side-req is fulfilled. But we should wait for the main order status.
          //first hide the side-request
            sideGO.GetComponent<Renderer>().enabled = false;
            fulfilledRequestType |= CustomerRequestType.Side;
            return CustomerRequestResponseType.Correct;
        }
        return CustomerRequestResponseType.Leave;
    }

    public bool IsRequestPicked(CustomerRequestType target)
    {
        return pickedRequestType.IsATypeof(target);
    }
    public bool IsAllRequestsFulfilled => pickedRequestType == fulfilledRequestType;

    public bool IsRequestFulfilled(CustomerRequestType target)
    {
        return fulfilledRequestType.IsATypeof(target);
    }

    public int CalculateRequestMoney()
    {
        //update 1.8.3 (2020)
        //to calculate the final money, first check if this order contains a sideRequest or maybe only consists of a sideRequest.
        //as we need to calculate the price of the sideReq items as well.
        int sideReqPrice = 0;
        int mainOrderPrice = 0;

         if (pickedRequestType.IsATypeof(CustomerRequestType.MainDish))
        {
            mainOrderPrice = customerData.availableMainDishes[customerMainDishReq].GetComponent<ProductManager>().price;

        }
        if (pickedRequestType.IsATypeof(CustomerRequestType.Side))
        {
            sideReqPrice = customerData.availableSides[customerSideReq].GetComponent<SideRequestMover>().sideReqPrice;

        }
       return sideReqPrice + mainOrderPrice;
            
    }

    public void ReachedSeat()
    {
        requestBubble.SetActive(true);
    }

    public void UpdateVisual()
    {
        if(mainDishGO) mainDishGO.GetComponent<Renderer>().enabled = !IsRequestFulfilled(CustomerRequestType.MainDish);
        if(sideGO) sideGO.GetComponent<Renderer>().enabled = !IsRequestFulfilled(CustomerRequestType.Side);
    }
}

public static class CustomerRequestTypeExtensions
{
    public static bool IsATypeof(this CustomerRequestController.CustomerRequestType self,CustomerRequestController.CustomerRequestType targetType)
    {
        return (self & targetType) != 0;
    }
}