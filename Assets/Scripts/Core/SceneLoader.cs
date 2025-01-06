using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core
{
    public class SceneLoader : MonoBehaviour
    {
        [SerializeField] private Button websocketChatButton;
        //[SerializeField] private Button webServicesButton;
    
        private void Start()
        {
            websocketChatButton.onClick.AddListener(() => StartCoroutine(LoadSceneAsync("WebsocketChatExperiment")));
            //webServicesButton.onClick.AddListener(() => StartCoroutine(LoadSceneAsync("UnityWebServices")));
        }

        private IEnumerator LoadSceneAsync(string sceneName)
        {
            AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
            asyncLoad.allowSceneActivation = true;

            while (!asyncLoad.isDone)
            {
                // float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
                yield return null;
            }
        }

        private void OnDestroy()
        {
            websocketChatButton.onClick.RemoveAllListeners();
            //webServicesButton.onClick.RemoveAllListeners();
        }
    }
}