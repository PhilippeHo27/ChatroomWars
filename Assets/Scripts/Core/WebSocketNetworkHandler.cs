using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using NativeWebSocket;

namespace Core
{
    public class WebSocketNetworkHandler : IndestructibleSingletonBehaviour<WebSocketNetworkHandler>
    {
        private WebSocket _webSocket;
        [SerializeField] private WebSocketChatHandler chatHandler;
        private const string ServerUrlHttPs = "wss://sargaz.popnux.com/ws";
        private const string ServerUrlHttp = "ws://18.226.150.199:8080";
        private readonly Queue<UnityAction> _actions = new Queue<UnityAction>();

        private void Update()
        {
            #if !UNITY_WEBGL || UNITY_EDITOR
            if (_webSocket != null)
                _webSocket.DispatchMessageQueue();
            #endif

            // Process main thread actions
            lock(_actions)
            {
                while (_actions.Count > 0)
                {
                    _actions.Dequeue().Invoke();
                }
            }
        }

        private void EnqueueMainThread(UnityAction action)
        {
            lock(_actions)
            {
                _actions.Enqueue(action);
            }
        }

        async public void Connect(string s)
        {
            var serverUrl = "";
            if (s == "https")
            {
                serverUrl = ServerUrlHttPs;
            }
            else if (s == "http")
            {
                serverUrl = ServerUrlHttp;
            }

            _webSocket = new WebSocket(serverUrl);
            
            _webSocket.OnMessage += HandleMessage;
            _webSocket.OnOpen += HandleOpen;
            _webSocket.OnError += HandleError;

            await _webSocket.Connect();
        }

        private void HandleOpen()
        {
            Debug.Log("Connected");
        }

        private void HandleError(string error)
        {
            Debug.LogError($"WebSocket Error: {error}");
        }

        public async void SendWebSocketMessage(string message)
        {
            if (_webSocket != null && _webSocket.State == WebSocketState.Open)
            {
                await _webSocket.SendText(message);
            }
            else
            {
                Debug.LogWarning("WebSocket is not connected. Cannot send message.");
            }
        }

        private void HandleMessage(byte[] data)
        {
            try
            {
                var message = System.Text.Encoding.UTF8.GetString(data);
                Debug.Log($"Received message: {message}");
                
                EnqueueMainThread(() => {
                    if (chatHandler != null)
                    {
                        chatHandler.DisplayMessage(message);
                        Canvas.ForceUpdateCanvases();
                    }
                    else
                    {
                        Debug.LogError("ChatHandler reference is missing!");
                    }
                });
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Error processing message: {e.Message}");
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