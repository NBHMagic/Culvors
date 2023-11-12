using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu]
public class CustomerReferences : ScriptableObject
{
    public AudioClip orderIsOkSfx;
    public AudioClip orderIsNotOkSfx;
    public AudioClip receivedSomethingGoodSfx;

    public Material[] customerMoods;
}
