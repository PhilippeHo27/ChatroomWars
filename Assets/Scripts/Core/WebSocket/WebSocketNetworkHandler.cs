using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;
using Debug = UnityEngine.Debug;

namespace Core.WebSocket
{
    public class WebSocketNetworkHandler : IndestructibleSingletonBehaviour<WebSocketNetworkHandler>
    {
        private NativeWebSocket.WebSocket _webSocket;
        [SerializeField] private WebSocketChatHandler chatHandler;
        public WebSocketChatHandler ChatHandler => chatHandler;

        [SerializeField] private WebSocketMovementHandler movementHandler;
        public WebSocketMovementHandler MovementHandler => movementHandler;
        
        private const string ServerUrlHttPs = "wss://sargaz.popnux.com/ws";
        private const string ServerUrlHttp = "ws://18.226.150.199:8080";
        private readonly Queue<Action> _actions = new Queue<Action>();
        
        private Dictionary<PacketType, Action<string>> _messageHandlers;
        private byte _clientId;
        public byte ClientId => _clientId;
        private bool _isConnecting;
        private ushort _currentSequence;

        
        protected override void Awake()
        {
            base.Awake();
            InitializeMessageHandlers();
        }
        private void InitializeMessageHandlers()
        {
            _messageHandlers = new Dictionary<PacketType, Action<string>>
            {
                { PacketType.Chat, ProcessChatMessage },
                { PacketType.Position, ProcessPosition },
                // Add more handlers here as needed
            };
        }
        private void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            _webSocket.DispatchMessageQueue();    
            #endif

            lock(_actions)
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue().Invoke();
                }
            }
        }
        
        #region Connection Management
        public void Connect(string s)
        {
            // Check if already connected or in the process of connecting
            if (_webSocket != null && 
                (_webSocket.State == WebSocketState.Open || 
                 _webSocket.State == WebSocketState.Connecting || 
                 _isConnecting))
            {
                Debug.Log("Already connected or connecting. Ignoring connection request.");
                ConsoleLogManager.Instance.Log("Already connected or connecting. Ignoring connection request.");
                return;
            }

            _isConnecting = true;
            ConnectAsync(s).ContinueWith(task => 
            {
                _isConnecting = false;
                if (task.IsFaulted)
                {
                    Debug.LogError($"Connection failed: {task.Exception}");
                    ConsoleLogManager.Instance.Log($"Connection failed: {task.Exception}");
                }
            });
        }
        private async Task ConnectAsync(string s)
        {
            try 
            {
                if (_webSocket != null)
                {
                    await _webSocket.Close();
                    _webSocket = null;
                }

                var serverUrl = s == "https" ? ServerUrlHttPs : ServerUrlHttp;
                _webSocket = new NativeWebSocket.WebSocket(serverUrl);

                _webSocket.OnMessage += ProcessIncomingMessage;
                _webSocket.OnOpen += HandleOpen;
                _webSocket.OnError += HandleError;
                _webSocket.OnClose += closeCode => Debug.Log($"Connection closed: {closeCode}");
                _webSocket.OnClose += _ => Disconnect();
                
                await _webSocket.Connect();
            }
            catch (Exception e)
            {
                _isConnecting = false;
                Debug.LogError($"Connection error: {e.Message}");
                ConsoleLogManager.Instance.Log($"Connection error: {e.Message}");
                throw;
            }
        }
        
        public async Task Disconnect()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.Close();
            }
        }
        private void HandleOpen()
        {
            Debug.Log("Connected");
            ConsoleLogManager.Instance.Log("WebSocket Connected");
        }
        private void HandleError(string error)
        {
            Debug.LogError($"WebSocket Error: {error}");
            ConsoleLogManager.Instance.Log($"WebSocket Error: {error}");
        }
        private void OnApplicationQuit()
        {
            CloseWebSocketConnection();
        }

        private void CloseWebSocketConnection()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                StartCoroutine(CloseWebSocketCoroutine());
            }
        }

        private IEnumerator CloseWebSocketCoroutine()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                var closeTask = _webSocket.Close();
                while (!closeTask.IsCompleted)
                {
                    yield return null;
                }

                if (closeTask.Exception != null)
                {
                    Debug.LogError($"Error closing WebSocket: {closeTask.Exception}");
                }
            }
        }
        #endregion
        
        #region Sending

        public ushort GetNextSequenceNumber()
        {
            // Will automatically wrap around to 0 when it exceeds ushort.MaxValue (65535)
            return _currentSequence++;
        }
        
        public void SendWebSocketPackage(BaseWebSocketPackage package)
        {
            // Ensure the SenderId is set correctly
            package.SenderId = _clientId;
    
            // Serialize the package
            string jsonMessage = JsonUtility.ToJson(package);

            SendWebSocketPackageAsync(jsonMessage).ContinueWith(task => 
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to send message: {task.Exception}");
                    ConsoleLogManager.Instance.Log($"Failed to send message: {task.Exception}");
                }
            });
        }

        private async Task SendWebSocketPackageAsync(string message)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.SendText(message);
            }
            else
            {
                throw new InvalidOperationException("WebSocket is not connected. Cannot send message.");
            }
        }

        #endregion

        #region Receiving

        private void EnqueueMainThread(Action action)
        {
            lock(_actions)
            {
                _actions.Enqueue(action);
            }
        }
        private void ProcessIncomingMessage(byte[] data)
        {
            try
            {
                var message = System.Text.Encoding.UTF8.GetString(data);
                var baseMessage = JsonUtility.FromJson<BaseWebSocketPackage>(message);
                if (_messageHandlers.TryGetValue(baseMessage.Type, out var handler))
                {
                    Debug.Log($"Handler found for type {baseMessage.Type}, enqueueing");
                    EnqueueMainThread(() => handler(message));
                }
                else
                {
                    Debug.Log($"No handler found for message type: {baseMessage.Type}");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Error processing message: {e.Message}");
            }
        }
        private void ProcessChatMessage(string jsonWebSocketMessage) =>
            chatHandler?.ProcessIncomingChatData(jsonWebSocketMessage);
        private void ProcessPosition(string jsonWebSocketMessage) =>
            movementHandler?.ProcessRemotePositionUpdate(jsonWebSocketMessage);
        #endregion
    }
}

// ConsoleLogManager.Instance.Log($"Error processing message: {e.Message}");
