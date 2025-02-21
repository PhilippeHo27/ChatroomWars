using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using Core.VinceGame;
using NativeWebSocket;
using Debug = UnityEngine.Debug;
using MessagePack;

namespace Core.WebSocket
{
    public class WebSocketNetworkHandler : IndestructibleSingletonBehaviour<WebSocketNetworkHandler>
    {
        // ## Constants
        private const string ServerUrlHttPs = "wss://sargaz.popnux.com/ws";
        private const string ServerUrlHttp = "ws://18.226.150.199:8080"; 
        private const string ServerUrlLocal = "ws://localhost:8080";

        // ## Core Components
        private NativeWebSocket.WebSocket _webSocket;
        public bool IsConnected => _webSocket?.State == WebSocketState.Open;
        private bool _isConnecting;

        // ## Handlers
        private ChatHandler _chatHandler;
        public ChatHandler ChatHandler { get => _chatHandler; set => _chatHandler = value; }

        private MovementHandler _movementHandler;
        public MovementHandler MovementHandler { get => _movementHandler; set => _movementHandler = value; }
        
        private GamePrototype _vinceGame;
        public GamePrototype VinceGame { get => _vinceGame; set => _vinceGame = value; }

        // ## Message Processing
        private readonly Queue<Action> _actions = new Queue<Action>();
        private Dictionary<PacketType, Action<byte[]>> _messageHandlers;
        private readonly MessagePackConfig _messagePackConfig = new MessagePackConfig();

        // ## User Management
        private byte _clientId;
        public byte ClientId => _clientId;
        private readonly Dictionary<byte, string> _users = new Dictionary<byte, string>();
        public IReadOnlyDictionary<byte, string> Users => _users;

        // ## Events
        public event Action<bool> OnServerResponse;
        public event Action OnGameReadyResponse;
        
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
                { PacketType.ServerResponse, HandleServerResponse },
                { PacketType.UserInfo, HandleUserInfo }

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

                _webSocket = new NativeWebSocket.WebSocket(ServerUrlHttPs);
                //_webSocket = new NativeWebSocket.WebSocket(ServerUrlLocal);

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
        
        public void SendWebSocketPackage<T>(T package) where T : BaseNetworkPacket
        {
            if (_webSocket == null || _webSocket.State != WebSocketState.Open)
            {
                Debug.LogError("Cannot send message: WebSocket is not connected");
                return;
            }

            package.SenderId = _clientId;

            try 
            {
                byte[] bytes = MessagePackSerializer.Serialize(package);
                SendWebSocketPackageAsync(bytes).ContinueWith(task => 
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError($"Send failed: {task.Exception?.Message}");
                    }
                });
            }
            catch (Exception ex)
            {
                Debug.LogError($"Serialization failed: {ex}");
            }
        }
        
        private void LogPackageDebugInfo(BaseNetworkPacket package)
        {
            Debug.Log($"Sending package of type: {package.GetType().Name}");
            Debug.Log($"Package contents: SenderId={package.SenderId}, Type={package.Type}");
    
            if (package is StringPacket chatData)
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
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded == null || decoded.Length < 2) return;

            var packetType = (PacketType)Convert.ToInt32(decoded[1]);
    
            if (_messageHandlers.TryGetValue(packetType, out var handler))
            {
                EnqueueMainThread(() => handler(data));
            }
        }

        private void ProcessChatMessage(byte[] messagePackData) =>
            _chatHandler?.ProcessIncomingChatData(messagePackData);
        private void ProcessPosition(byte[] messagePackData) =>
            _movementHandler?.ProcessRemotePositionUpdate(messagePackData);
        private void ProcessVinceGame(byte[] messagePackData) => 
            _vinceGame?.ReceiveMove(messagePackData);
        
        private void HandleIdAssign(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                _clientId = (byte)decoded[2];
                //Debug.Log($"Assigned Client ID: {_clientId}");
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
                int packetType = Convert.ToInt32(decoded[1]);
                bool response = Convert.ToBoolean(decoded[2]);
        
                switch(packetType)
                {
                    case 8: // SERVER_RESPONSE
                        OnServerResponse?.Invoke(response);
                        break;
                    case 12: // VINCE_GAME_CONFIRM_START
                        OnGameReadyResponse?.Invoke();
                        break;
                }
            }
        }
        
        private void HandleUserInfo(byte[] data)
        {
            var decoded = MessagePackSerializer.Deserialize<object[]>(data);
            if (decoded != null && decoded.Length >= 3)
            {
                var userList = MessagePackSerializer.Deserialize<UserEntry[]>(MessagePackSerializer.Serialize(decoded[2]));
                if (userList != null)
                {
                    foreach (var user in userList)
                    {
                        _users[user.UserId] = user.UserName;
                    }
                }
            }
        }
        
        #endregion
    }
}