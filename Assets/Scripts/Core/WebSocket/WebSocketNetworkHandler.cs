using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;
using NativeWebSocket;
using UnityEngine;
using Debug = UnityEngine.Debug;
using MessagePack;

namespace Core.WebSocket
{
    public class WebSocketNetworkHandler : IndestructibleSingletonBehaviour<WebSocketNetworkHandler>
    {
        private NativeWebSocket.WebSocket _webSocket;
        private ChatHandler _chatHandler;
        public ChatHandler ChatHandler { get => _chatHandler; set => _chatHandler = value; }

        private MovementHandler _movementHandler;
        public MovementHandler MovementHandler { get => _movementHandler; set => _movementHandler = value; }
        
        private const string ServerUrlHttPs = "wss://sargaz.popnux.com/ws";
        private const string ServerUrlHttp = "ws://18.226.150.199:8080";
        private const string ServerUrlLocal = "ws://localhost:8080";

        private readonly Queue<Action> _actions = new Queue<Action>();
        
        private Dictionary<PacketType, Action<byte[]>> _messageHandlers;
        private byte _clientId;
        public byte ClientId => _clientId;
        private bool _isConnecting;
        public event Action<bool> OnServerResponse;
        
        private readonly MessagePackConfig _messagePackConfig = new MessagePackConfig();
        
        protected override void Awake()
        {
            base.Awake();
            InitializeMessageHandlers();
            _messagePackConfig.InitMessagePackResolvers();
        }
        private void InitializeMessageHandlers()
        {
            _messageHandlers = new Dictionary<PacketType, Action<byte[]>>
            {
                { PacketType.Chat, ProcessChatMessage },
                { PacketType.Position, ProcessPosition },
                { PacketType.IdAssign, HandleIdAssign },
                { PacketType.TimeSync, HandleTimeSync },
                { PacketType.ServerResponse, HandleServerResponse }
            };
        }

        private void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            if(_webSocket != null)
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
        public void Connect()
        {
            // Check if already connected or in the process of connecting
            if (_webSocket != null && 
                (_webSocket.State == WebSocketState.Open || 
                 _webSocket.State == WebSocketState.Connecting || 
                 _isConnecting))
            {
                Debug.Log("Already connected or connecting. Ignoring connection request.");
                return;
            }

            _isConnecting = true;
            ConnectAsync().ContinueWith(task => 
            {
                _isConnecting = false;
                if (task.IsFaulted)
                {
                    Debug.LogError($"Connection failed: {task.Exception}");
                }
            });
        }
        private async Task ConnectAsync()
        {
            try 
            {
                if (_webSocket != null)
                {
                    await _webSocket.Close();
                    _webSocket = null;
                }

                //_webSocket = new NativeWebSocket.WebSocket(ServerUrlHttPs);
                _webSocket = new NativeWebSocket.WebSocket(ServerUrlLocal);

                _webSocket.OnMessage += ProcessIncomingMessage;
                _webSocket.OnOpen += HandleOpen;
                _webSocket.OnError += HandleError;
                
                await _webSocket.Connect();
            }
            catch (Exception e)
            {
                _isConnecting = false;
                Debug.LogError($"Connection error: {e.Message}");
                throw;
            }
        }

        private void HandleOpen()
        {
            Debug.Log("Connected");
        }
        private void HandleError(string error)
        {
            Debug.LogError($"WebSocket Error: {error}");
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
        
        public void SendWebSocketPackage(BaseWebSocketPackage package)
        {
            package.SenderId = _clientId;
            byte[] bytes = MessagePackSerializer.Serialize(package.GetType(), package);
            
            //LogPackageDebugInfo(package);

            SendWebSocketPackageAsync(bytes).ContinueWith(task => 
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to send message: {task.Exception}");
                }
            });
        }

        private void LogPackageDebugInfo(BaseWebSocketPackage package)
        {
            Debug.Log($"Sending package of type: {package.GetType().Name}");
            Debug.Log($"Package contents: SenderId={package.SenderId}, Type={package.Type}");
    
            if (package is ChatData chatData)
            {
                Debug.Log($"Chat text: {chatData.Text}");
            }
    
            byte[] bytes = MessagePackSerializer.Serialize(package.GetType(), package);
            Debug.Log($"Serialized bytes: [{string.Join(", ", bytes)}]");
        }

        
        private async Task SendWebSocketPackageAsync(byte[] bytes)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.Send(bytes);
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
                // Decode as array (MessagePack-encoded) 
                var decoded = MessagePackSerializer.Deserialize<object[]>(data);
                if (decoded == null || decoded.Length < 2) return;

                // Safely convert index 1 into a PacketType
                var typeValue = Convert.ToInt32(decoded[1]);
                var packetType = (PacketType)typeValue;

                //Debug.Log($"Received message type: {packetType}, array length: {decoded.Length}");

                if (_messageHandlers.TryGetValue(packetType, out var handler))
                {
                    //Debug.Log($"Handler found for type {packetType}, enqueueing");
                    EnqueueMainThread(() => handler(data));
                }
                else
                {
                    Debug.Log($"No handler found for message type: {packetType}");
                }
            }
            catch (Exception e)
            {
                Debug.Log($"Error processing MessagePack data: {e.Message}\nStack: {e.StackTrace}");
            }
        }


        private void ProcessChatMessage(byte[] messagePackData) =>
            _chatHandler?.ProcessIncomingChatData(messagePackData);

        private void ProcessPosition(byte[] messagePackData) =>
            _movementHandler?.ProcessRemotePositionUpdate(messagePackData);
        
        private void HandleIdAssign(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                _clientId = (byte)decoded[2];
                Debug.Log($"Assigned Client ID: {_clientId}");
            }
        }

        private void HandleTimeSync(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                long serverTime = Convert.ToInt64(decoded[2]);
                // Use serverTime as needed
                //Debug.Log($"Time Sync Received: {serverTime}");
            }
        }

        private void HandleServerResponse(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                Debug.Log($"Decoded array contents: [{string.Join(", ", decoded)}]");
                Debug.Log($"Type of decoded[2]: {decoded[2]?.GetType().Name}");
        
                bool serverResponse = Convert.ToBoolean(decoded[2]);
                Debug.Log($"Server Response: {serverResponse}");
        
                OnServerResponse?.Invoke(serverResponse);
            }
        }


        #endregion
    }
}
