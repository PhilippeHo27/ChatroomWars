using UnityEngine;
using UnityEngine.InputSystem;
using Core.WebSocket;

namespace Core
{
    public class DraggableObject : MonoBehaviour
    {
        private bool _isDragging = false;
        private Vector3 _offset;
        private Camera _mainCamera;
        private WebSocketNetworkHandler _wsHandler;
        private WebSocketMovementHandler _movementHandler;

        private string _objectId;
        private Vector3 _lastSentPosition;
        private float _sendThreshold = 0.01f; // Minimum distance to move before sending an update
        private float _sendInterval = 0.05f; // Send at most every 50ms
        private float _lastSendTime;

        private void Start()
        {
            _mainCamera = Camera.main;
            _wsHandler = WebSocketNetworkHandler.Instance;
            _movementHandler = _wsHandler.MovementHandler;

            _objectId = gameObject.name; // Use the object's name as its ID
            
            InputManager.Instance.InputActionHandlers["PlayerClick"].Started += OnLeftClickStarted;
            InputManager.Instance.InputActionHandlers["PlayerClick"].Canceled += OnLeftClickCanceled;
            InputManager.Instance.GameControls.UI.Enable();

            // Register this object with the WebSocketMovementHandler
            _wsHandler.MovementHandler.RegisterObject(_objectId, gameObject);
        }

        private void OnDisable()
        {
            InputManager.Instance.InputActionHandlers["PlayerClick"].Started -= OnLeftClickStarted;
            InputManager.Instance.InputActionHandlers["PlayerClick"].Canceled -= OnLeftClickCanceled;
        }

        private void OnLeftClickStarted(InputAction.CallbackContext context)
        {
            Vector2 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(mousePosition, Vector2.zero);
    
            if (hit.collider != null && hit.collider.gameObject == gameObject)
            {
                _isDragging = true;
                _offset = transform.position - (Vector3)mousePosition;
                _lastSentPosition = transform.position;
                _lastSendTime = Time.time;
            }
        }
        private void OnLeftClickCanceled(InputAction.CallbackContext context)
        {
            _isDragging = false;
        }

        private void Update()
        {
            if (_isDragging)
            {
                Vector3 mousePosition = _mainCamera.ScreenToWorldPoint(Input.mousePosition);
                mousePosition.z = transform.position.z;
                transform.position = mousePosition + _offset;

                if (ShouldSendUpdate())
                {
                    SendPositionUpdate();
                }
            }
        }

        private bool ShouldSendUpdate()
        {
            return Vector3.Distance(transform.position, _lastSentPosition) > _sendThreshold && Time.time - _lastSendTime > _sendInterval;
        }

        private void SendPositionUpdate()
        {
            _movementHandler.SendPositionUpdate(_objectId, transform.position);
            _lastSentPosition = transform.position;
            _lastSendTime = Time.time;
        }

        private void OnDestroy()
        {
            _movementHandler.UnregisterObject(_objectId);
        }
    }
}
