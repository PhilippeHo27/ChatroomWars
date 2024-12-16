using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;

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
        
        private Dictionary<string, Action<string>> _messageHandlers;

        protected override void Awake()
        {
            base.Awake();
            InitializeMessageHandlers();
        }

        private void InitializeMessageHandlers()
        {
            _messageHandlers = new Dictionary<string, Action<string>>
            {
                { "position", HandlePosition },
                { "chat", HandleChat },
                // Add more handlers here as needed
            };
        }

        private void Update()
        {
            lock(_actions)
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue().Invoke();
                }
            }
        }

        private void EnqueueMainThread(Action action)
        {
            lock(_actions)
            {
                _actions.Enqueue(action);
            }
        }

        public void Connect(string s)
        {
            ConnectAsync(s).ContinueWith(task => 
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Connection failed: {task.Exception}");
                    ConsoleLogManager.Instance.Log($"Connection failed: {task.Exception}");
                }
            });
        }

        private async Task ConnectAsync(string s)
        {
            var serverUrl = s == "https" ? ServerUrlHttPs : ServerUrlHttp;

            _webSocket = new NativeWebSocket.WebSocket(serverUrl);
    
            _webSocket.OnMessage += HandleMessage;
            _webSocket.OnOpen += HandleOpen;
            _webSocket.OnError += HandleError;

            await _webSocket.Connect();
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

        public void SendWebSocketMessage(string message)
        {
            SendWebSocketMessageAsync(message).ContinueWith(task => 
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to send message: {task.Exception}");
                    ConsoleLogManager.Instance.Log($"Failed to send message: {task.Exception}");
                }
            });
        }

        private async Task SendWebSocketMessageAsync(string message)
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

        private void HandleMessage(byte[] data)
        {
            try
            {
                var message = System.Text.Encoding.UTF8.GetString(data);
                Debug.Log($"Received message: {message}");
                ConsoleLogManager.Instance.Log($"Received message: {message}");
                
                var baseMessage = JsonUtility.FromJson<WebSocketPackage>(message);
                if (_messageHandlers.TryGetValue(baseMessage.type, out var handler))
                {
                    EnqueueMainThread(() => handler(message));
                }
                else
                {
                    Debug.LogWarning($"No handler found for message type: {baseMessage.type}");
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Error processing message: {e.Message}");
                ConsoleLogManager.Instance.Log($"Error processing message: {e.Message}");
            }
        }

        private void HandlePosition(string jsonMessage)
        {
            var positionData = JsonUtility.FromJson<PositionData>(jsonMessage);
            if (movementHandler != null)
            {
                movementHandler.HandlePositionUpdate(jsonMessage);
            }
            else
            {
                Debug.LogError("MovementHandler reference is missing!");
                ConsoleLogManager.Instance.Log("MovementHandler reference is missing!");
            }
        }


        private void HandleChat(string jsonMessage)
        {
            Debug.Log($"Handling chat message: {jsonMessage}");
            if (chatHandler != null)
            {
                chatHandler.ProcessIncomingChatData(jsonMessage);
            }
            else
            {
                Debug.LogError("ChatHandler reference is missing!");
                ConsoleLogManager.Instance.Log("ChatHandler reference is missing!");
            }
        }
        private async void OnApplicationQuit()
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.Close();
            }
        }
    }
}
