using System;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    public class IntroScreenCanvas : MonoBehaviour
    {
        [SerializeField] private Button hostButton;
        [SerializeField] private Button clientButton;
        
        [SerializeField] private Button connectButton;
        //[SerializeField] private WebSocketNetworkHandler networkHandler;
        private void Start()
        {
            // hostButton.onClick.AddListener(() => NetworkHandler.Instance.StartHost());
            // clientButton.onClick.AddListener(() => NetworkHandler.Instance.StartClient());
            
            connectButton.onClick.AddListener(() => WebSocketNetworkHandler.Instance.Connect());

        }
    }
}
