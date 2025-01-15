    using Core.WebSocket;
    using UnityEngine;
    using UnityEngine.Serialization;
    using UnityEngine.UI;

    namespace Core
    {
        public class ChatroomCanvas : MonoBehaviour
        {
            [SerializeField] private Button connectButton;
            private void Start()
            {
                connectButton.onClick.AddListener(() => WebSocketNetworkHandler.Instance.Connect());
            }
        }
    }
