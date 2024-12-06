using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class LoadClientButton : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
     GetComponent<Button>().onClick.AddListener(HandleButtonClick); }

    void HandleButtonClick()
    {
        Debug.Log("Load client button clicked");
        
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Client");
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
