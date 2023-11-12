using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UniRx;
using UnityEngine.SceneManagement;
public class MainMenuController : MonoBehaviour
{
    [SerializeField] Button shopButton,iapButton,settingsButton, playButton, comingSoonButton;


    // Start is called before the first frame update
    void Start()
    {
        shopButton.OnClickAsObservable().Subscribe(_=> OnShopButtonClicked()).AddTo(this);
        iapButton.OnClickAsObservable().Subscribe(_=> OnIAPButtonClicked()).AddTo(this);
        settingsButton.OnClickAsObservable().Subscribe(_=> OnSettingsButtonClicked()).AddTo(this);
        playButton.OnClickAsObservable().Subscribe(_=> OnPlayButtonClicked()).AddTo(this);
        comingSoonButton.OnClickAsObservable().Subscribe(_=> OnComingSoonButtonClicked()).AddTo(this);
    }

    void OnShopButtonClicked()
    {
        SceneManager.LoadScene("Shop-c#");
    }
    void OnIAPButtonClicked()
    {
        SceneManager.LoadScene("BuyCoinPack-c#");
    }
    void OnSettingsButtonClicked()
    {

    }
    void OnPlayButtonClicked()
    {
        PlayerPrefs.SetString("gameMode", "FREEPLAY");	//set game mode to fetch later in "Game" scene
        SceneManager.LoadScene("Game-c#");			
    }
    void OnComingSoonButtonClicked()
    {

    }
}
