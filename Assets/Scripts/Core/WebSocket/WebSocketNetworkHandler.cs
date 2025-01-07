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
        [SerializeField] private WebSocketChatHandler chatHandler;
        public WebSocketChatHandler ChatHandler => chatHandler;

        [SerializeField] private WebSocketMovementHandler movementHandler;
        public WebSocketMovementHandler MovementHandler => movementHandler;
        
        private const string ServerUrlHttPs = "wss://sargaz.popnux.com/ws";
        private const string ServerUrlHttp = "ws://18.226.150.199:8080";
        private readonly Queue<Action> _actions = new Queue<Action>();
        
        private Dictionary<PacketType, Action<byte[]>> _messageHandlers;
        private byte _clientId;
        public byte ClientId => _clientId;
        private bool _isConnecting;
        private ushort _currentSequence;

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
                { PacketType.TimeSync, HandleTimeSync }
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
            package.SenderId = _clientId;
    
            // Debug logging
            Debug.Log($"Sending package of type: {package.GetType().Name}");
            Debug.Log($"Package contents: SenderId={package.SenderId}, Type={package.Type}, Sequence={package.Sequence}");
            if (package is ChatData chatData)
            {
                Debug.Log($"Chat text: {chatData.Text}");
            }
    
            //var options = MessagePackSerializerOptions.Standard;
            byte[] bytes = MessagePackSerializer.Serialize(package.GetType(), package);
    
            Debug.Log($"Serialized bytes: [{string.Join(", ", bytes)}]");

            SendWebSocketPackageAsync(bytes).ContinueWith(task => 
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Failed to send message: {task.Exception}");
                }
            });
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
        
        
        // public void SendWebSocketPackage(BaseWebSocketPackage package)
        // {
        //     // Ensure the SenderId is set correctly
        //     package.SenderId = _clientId;
        //
        //     // Serialize the package
        //     string jsonMessage = JsonUtility.ToJson(package);
        //
        //     SendWebSocketPackageAsync(jsonMessage).ContinueWith(task => 
        //     {
        //         if (task.IsFaulted)
        //         {
        //             Debug.LogError($"Failed to send message: {task.Exception}");
        //             ConsoleLogManager.Instance.Log($"Failed to send message: {task.Exception}");
        //         }
        //     });
        // }
        //
        // private async Task SendWebSocketPackageAsync(string message)
        // {
        //     if (_webSocket != null && _webSocket.State == WebSocketState.Open)
        //     {
        //         await _webSocket.SendText(message);
        //     }
        //     else
        //     {
        //         throw new InvalidOperationException("WebSocket is not connected. Cannot send message.");
        //     }
        // }

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

                Debug.Log($"Received message type: {packetType}, array length: {decoded.Length}");

                if (_messageHandlers.TryGetValue(packetType, out var handler))
                {
                    Debug.Log($"Handler found for type {packetType}, enqueueing");
                    EnqueueMainThread(() => handler(data));  // pass byte[] so handler can do detailed deserialization
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
            chatHandler?.ProcessIncomingChatData(messagePackData);

        private void ProcessPosition(byte[] messagePackData) =>
            movementHandler?.ProcessRemotePositionUpdate(messagePackData);
        
        private void HandleIdAssign(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 4)
            {
                _clientId = (byte)decoded[3];
                Debug.Log($"Assigned Client ID: {_clientId}");
            }
        }

        private void HandleTimeSync(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 4)
            {
                long serverTime = Convert.ToInt64(decoded[3]);
                // Use serverTime as needed
                Debug.Log($"Time Sync Received: {serverTime}");
            }
        }
        #endregion
    }
}

// ConsoleLogManager.Instance.Log($"Error processing message: {e.Message}");
