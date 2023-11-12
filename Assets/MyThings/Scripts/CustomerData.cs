using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CustomerData : ScriptableObject
{
    public GameObject[] availableMainDishes;      //list of all available product to choose from

    public GameObject[] availableSides;      //List of all available side-request items
    public DictionaryIntFloat customerRequestProbability = new DictionaryIntFloat();

    public float customerPatience = 30.0f;              //seconds (default = 30 or whatever you wish)
    public float customerMoveSpeed = 3f;

    public bool showProductIngredientHelpers = true;    //whether to show customer wish's ingredeints as a helper or not (player has to guess)


    public string[] messages;
}
