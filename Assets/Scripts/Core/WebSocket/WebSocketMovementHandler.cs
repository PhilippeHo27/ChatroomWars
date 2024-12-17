using System.Collections.Generic;
using UnityEngine;

namespace Core.WebSocket
{
    public class WebSocketMovementHandler : MonoBehaviour
    {
        [SerializeField] private WebSocketNetworkHandler wsHandler;
        private Dictionary<string, GameObject> _trackedObjects = new Dictionary<string, GameObject>();
        private Dictionary<string, Vector3> _lastSentPositions = new Dictionary<string, Vector3>();
        private const float MOVEMENT_THRESHOLD = 0.001f; // Adjust this value as needed
        public void RegisterObject(string objectId, GameObject obj)
        {
            if (!_trackedObjects.ContainsKey(objectId))
            {
                _trackedObjects.Add(objectId, obj);
            }
        }

        public void UnregisterObject(string objectId)
        {
            if (_trackedObjects.ContainsKey(objectId))
            {
                _trackedObjects.Remove(objectId);
            }
        }

        public void SendPositionUpdate(string objectId, Vector3 position)
        {
            // Only send if position has actually changed
            if (!_lastSentPositions.ContainsKey(objectId) || 
                Vector3.Distance(_lastSentPositions[objectId], position) > MOVEMENT_THRESHOLD)
            {
                var positionMessage = new PositionData
                {
                    Type = PacketType.Position,
                    ObjectId = objectId,
                    SenderId = wsHandler.ClientId,
                    X = position.x,
                    Y = position.y,
                    Z = position.z
                };
            
                _lastSentPositions[objectId] = position;
                wsHandler.SendWebSocketPackage(positionMessage);
            }
        }

        public void ProcessRemotePositionUpdate(string jsonMessage)
        {
            var positionData = JsonUtility.FromJson<PositionData>(jsonMessage);
            if (positionData.SenderId == wsHandler.ClientId) return; // Ignore own updates
    
            if (_trackedObjects.TryGetValue(positionData.ObjectId, out GameObject obj))
            {
                // Optional: Add interpolation for smoother movement
                obj.transform.position = new Vector3(
                    positionData.X,
                    positionData.Y,
                    positionData.Z
                );
            }
        }


        private void OnDestroy()
        {
            _trackedObjects.Clear();
        }
    }
}