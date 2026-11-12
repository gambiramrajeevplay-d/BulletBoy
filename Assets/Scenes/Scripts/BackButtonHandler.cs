using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BackButtonHandler : MonoBehaviour
{
    public GameObject exitPopup;
    public GameObject soundOffSource;
    //public string homeSceneName = "SpalshScreen"; // Mee Home scene name

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            exitPopup.SetActive(true); // Show popup
            soundOffSource.SetActive(false);
        }
    }

    public void OnClickYes()
    {
        Application.Quit();
    }

    public void OnClickNo()
    {
        exitPopup.SetActive(false);
        soundOffSource.SetActive(true);
    }
}
