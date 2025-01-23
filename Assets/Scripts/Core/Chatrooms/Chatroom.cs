using Core.WebSocket;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using DG.Tweening;

namespace Core
{
    public class Chatroom : MonoBehaviour
    {
        //[SerializeField] private Button connectButton;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private Button backButton;
        [SerializeField] private string roomId = "Lobby";
        [SerializeField] private TMP_InputField roomName;
        
        [SerializeField] private GameObject chatBox;
        [SerializeField] private TMP_Text waitResponseMessage;
        
        private PacketType? _awaitingActionType;
        
        private void Start()
        {
            //connectButton.onClick.AddListener(() => WebSocketNetworkHandler.Instance.Connect());
            WebSocketNetworkHandler.Instance.Connect();
            
            createRoomButton.onClick.AddListener(CreateRoom);
            joinRoomButton.onClick.AddListener(JoinRoom);
            backButton.onClick.AddListener(LeaveRoom);
            WebSocketNetworkHandler.Instance.OnServerResponse += HandleServerResponse;
        }
        
        private void CreateRoom()
        {
            var createData = new RoomAction
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomCreate,
                RoomId = string.IsNullOrEmpty(roomName.text) ? roomId : roomName.text
            };
        
            createRoomButton.gameObject.SetActive(false);
            roomName.gameObject.SetActive(false);
            joinRoomButton.gameObject.SetActive(false);
            _awaitingActionType = PacketType.RoomCreate;
            waitResponseMessage.text = "creating room...";
            FadeText(waitResponseMessage, true);

            
            Debug.Log("Creating room... ");
            HandleServerResponse(true); // TODO:: REMOVE THIS IT'S FOR DEBUG
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(createData);
        }

        private void JoinRoom()
        {
            var joinData = new RoomAction
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomJoin,
                RoomId = string.IsNullOrEmpty(roomName.text) ? roomId : roomName.text
            };

            createRoomButton.gameObject.SetActive(false);
            roomName.gameObject.SetActive(false);
            joinRoomButton.gameObject.SetActive(false);
            _awaitingActionType = PacketType.RoomJoin;
            waitResponseMessage.text = "joining room...";
            FadeText(waitResponseMessage, true);


            Debug.Log($"Attempting to join room: {roomId}");
            HandleServerResponse(true); // TODO:: REMOVE THIS IT'S FOR DEBUG
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(joinData);
        }
        
        // this is pinged when we do return/exit out of this mode
        private void LeaveRoom()
        {
            var leaveData = new RoomAction
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomLeave,
                RoomId = string.IsNullOrEmpty(roomName.text) ? roomId : roomName.text
            };

            _awaitingActionType = PacketType.RoomLeave;
            waitResponseMessage.text = "creating room...";
            FadeText(waitResponseMessage, true);

            Debug.Log($"Attempting to leave room: {roomId}");
            HandleServerResponse(true); // TODO:: REMOVE THIS IT'S FOR DEBUG
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(leaveData);
        }
        
        // I am not sure if this one is useful because the destruction of rooms should be handled by the server
        private void DestroyRoom()
        {
            var destroyData = new RoomAction
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomDestroy,
                RoomId = string.IsNullOrEmpty(roomName.text) ? roomId : roomName.text
            };

            _awaitingActionType = PacketType.RoomDestroy;
            Debug.Log($"Attempting to destroy room: {roomId}");

            WebSocketNetworkHandler.Instance.SendWebSocketPackage(destroyData);
        }

        private void HandleServerResponse(bool serverResponse)
        {
            if (!_awaitingActionType.HasValue) return;

            switch (_awaitingActionType.Value)
            {
                case PacketType.RoomCreate:
                    if (serverResponse)
                    {
                        Debug.Log("Room created successfully!");
                        chatBox.SetActive(true);
                        backButton.gameObject.SetActive(true);
                        waitResponseMessage.text = "";
                    }
                    else
                    {
                        Debug.Log("Room creation failed!");
                        waitResponseMessage.text = "Could not create room";
                        createRoomButton.gameObject.SetActive(true);
                        roomName.gameObject.SetActive(true);
                        joinRoomButton.gameObject.SetActive(true);
                    }
                    break;

                case PacketType.RoomJoin:
                    if (serverResponse)
                    {
                        Debug.Log("Successfully joined room!");
                        chatBox.SetActive(true);
                        backButton.gameObject.SetActive(true);
                        waitResponseMessage.text = "";
                    }
                    else
                    {
                        Debug.Log("Failed to join room!");
                        waitResponseMessage.text = "Could not join room";
                        createRoomButton.gameObject.SetActive(true);
                        roomName.gameObject.SetActive(true);
                        joinRoomButton.gameObject.SetActive(true);
                    }
                    break;

                case PacketType.RoomLeave:
                    if (serverResponse)
                    {
                        Debug.Log("Successfully left room!");
                        waitResponseMessage.text = " ";
                        backButton.gameObject.SetActive(false);
                        chatBox.SetActive(false);
                        createRoomButton.gameObject.SetActive(true);
                        roomName.gameObject.SetActive(true);
                        joinRoomButton.gameObject.SetActive(true);
                    }
                    else
                    {
                        Debug.Log("Failed to leave room! (Would show error TMP text here)");
                    }
                    break;

                // case PacketType.RoomDestroy:
                //     if (serverResponse)
                //     {
                //         Debug.Log("Successfully destroyed room! (Would deactivate chat box prefab here)");
                //         // setactive false chatBox prefab
                //     }
                //     else
                //     {
                //         Debug.Log("Failed to destroy room! (Would show error TMP text here)");
                //         // setactive true error tmp
                //     }
                //     break;
            }

            _awaitingActionType = null;

        }
        
        private void FadeText(TMP_Text textElement, bool fadeIn, float duration = 0.5f, float delay = 0f)
        {
            textElement.DOKill();
    
            float startValue = fadeIn ? 0f : 1f;
            float endValue = fadeIn ? 1f : 0f;
    
            textElement.alpha = startValue;
    
            textElement.DOFade(endValue, duration)
                .SetDelay(delay)
                .SetEase(Ease.InOutSine);
        }
        private void OnDestroy()
        {
            if (WebSocketNetworkHandler.Instance != null)
            {
                WebSocketNetworkHandler.Instance.OnServerResponse -= HandleServerResponse;
            }
        }
    }
}
