using Core.WebSocket;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Core
{
    public class ChatboxHandler : MonoBehaviour
    {
        [SerializeField] private Button joinRoomButton;
        [SerializeField] private string roomId = "TestRoom"; // The room to join

        private void Start()
        {
            // Attach the click handler if you have a button assigned
            if (joinRoomButton != null)
            {
                joinRoomButton.onClick.AddListener(OnJoinRoomClicked);
            }
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
        }


        private void OnJoinRoomClicked()
        {
            // Create our "join room" data
            var joinData = new RoomJoinData
            {
                // The network handler’s client ID
                SenderId = WebSocketNetworkHandler.Instance.ClientId,

                // Indicate it's a room join request
                Type = PacketType.RoomJoin,

                // Grab a sequence number from the network handler
                Sequence = WebSocketNetworkHandler.Instance.GetNextSequenceNumber(),

                // The actual room ID
                RoomId = roomId
            };

            // Send the packet
            WebSocketNetworkHandler.Instance.SendWebSocketPackage(joinData);

            Debug.Log($"Attempting to join room: {roomId}");
        }
    }
}