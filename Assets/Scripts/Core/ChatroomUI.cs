using Core.WebSocket;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Core
{
    public class ChatroomUI : MonoBehaviour
    {
        [SerializeField] private Button connectButton;
        [SerializeField] private Button createRoomButton;
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private string roomId = "TestRoom";

        private void Start()
        {
            joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            connectButton.onClick.AddListener(() => WebSocketNetworkHandler.Instance.Connect());
            createRoomButton.onClick.AddListener(CreateRoom);
        }
        
        private void CreateRoom()
        {
            var createData = new RoomCreateData
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomCreate,
                Sequence = WebSocketNetworkHandler.Instance.GetNextSequenceNumber(),
                RoomId = roomId
            };
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(createData);
            
            Debug.Log($"Attempting to create room: {roomId}");
        }


        private void OnJoinRoomClicked()
        {
            var joinData = new RoomJoinData
            {
                SenderId = WebSocketNetworkHandler.Instance.ClientId,
                Type = PacketType.RoomJoin,
                Sequence = WebSocketNetworkHandler.Instance.GetNextSequenceNumber(),
                RoomId = roomId
            };

            WebSocketNetworkHandler.Instance.SendWebSocketPackage(joinData);

            Debug.Log($"Attempting to join room: {roomId}");
        }
    }
}