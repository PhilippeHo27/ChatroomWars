using System.Collections.Generic;
using UnityEngine;

namespace Core.WebSocket
{
    public class WebSocketMovementHandler : MonoBehaviour
    {
        [SerializeField] private WebSocketNetworkHandler wsHandler;
        private Dictionary<string, GameObject> _trackedObjects = new Dictionary<string, GameObject>();
        
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
            var positionMessage = new PositionData
            {
                type = "position",
                objectId = objectId,
                x = position.x,
                y = position.y,
                z = position.z
            };
            
            string jsonMessage = JsonUtility.ToJson(positionMessage);
            wsHandler.SendWebSocketMessage(jsonMessage);
        }

        public void HandlePositionUpdate(string jsonMessage)
        {
            var positionData = JsonUtility.FromJson<PositionData>(jsonMessage);
    
            if (_trackedObjects.TryGetValue(positionData.objectId, out GameObject obj))
            {
                obj.transform.position = new Vector3(
                    positionData.x,
                    positionData.y,
                    positionData.z
                );
            }
            else
            {
                Debug.LogWarning($"No tracked object found with ID: {positionData.objectId}");
            }
        }
        private void OnDestroy()
        {
            _trackedObjects.Clear();
        }
    }
}