using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.Collections;

public class SceneLoader : MonoBehaviour
{
    [SerializeField] private Button websocketChatButton;
    [SerializeField] private Button webServicesButton;
    
    private void Start()
    {
        // Add click listeners to buttons
        websocketChatButton.onClick.AddListener(() => StartCoroutine(LoadSceneAsync("WebsocketChatExperiment")));
        webServicesButton.onClick.AddListener(() => StartCoroutine(LoadSceneAsync("UnityWebServices")));
    }

    private IEnumerator LoadSceneAsync(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = true;

        // Wait until the scene is fully loaded
        while (!asyncLoad.isDone)
        {
            // You can add loading progress here if needed
            // float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            yield return null;
        }
    }

    private void OnDestroy()
    {
        // Clean up listeners
        websocketChatButton.onClick.RemoveAllListeners();
        webServicesButton.onClick.RemoveAllListeners();
    }
}