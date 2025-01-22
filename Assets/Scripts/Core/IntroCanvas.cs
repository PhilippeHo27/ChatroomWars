using System.Collections;
using Core.Singletons;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Core
{
    public class IntroCanvas : MonoBehaviour
    {
        [SerializeField] private Button websocketChatButton;
        [SerializeField] private Button offlineScene;
    
        private void Start()
        {
            websocketChatButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("WebsocketChatExperiment"));
            offlineScene.onClick.AddListener(() => SceneLoader.Instance.LoadScene("OfflinePrototype"));
        }
        
        private void OnDestroy()
        {
            websocketChatButton.onClick.RemoveAllListeners();
            offlineScene.onClick.RemoveAllListeners();
        }
    }
}