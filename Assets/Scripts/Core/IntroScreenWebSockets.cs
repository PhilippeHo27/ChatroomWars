using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Core
{
    public class IntroScreenWebSockets : MonoBehaviour
    {
        [SerializeField] private Button connectHttPs;
        [SerializeField] private Button connectHttp;
        
        [SerializeField] private WebSocketNetworkHandler networkHandler;
        private void Start()
        {
            connectHttPs.onClick.AddListener(() => WebSocketNetworkHandler.Instance.Connect("https"));
            connectHttp.onClick.AddListener(() => WebSocketNetworkHandler.Instance.Connect("http"));
        }
    }
}
