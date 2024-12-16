using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

namespace Core.WebSocket
{
    public class WebSocketChatHandler : MonoBehaviour
    {
        [SerializeField] private WebSocketNetworkHandler wsHandler;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentPanel;
        [SerializeField] private TextMeshProUGUI chatTextPrefab;
        [SerializeField] private int poolSize = 100;

        private Queue<TextMeshProUGUI> _textComponentPool;
        private List<TextMeshProUGUI> _activeTextComponents;

        private void Start()
        {
            _textComponentPool = new Queue<TextMeshProUGUI>();
            _activeTextComponents = new List<TextMeshProUGUI>();
        
            InitializeTextComponentPool();
        }

        private void InitializeTextComponentPool()
        {
            for (int i = 0; i < poolSize; i++)
            {
                TextMeshProUGUI textMeshComponent = Instantiate(chatTextPrefab, contentPanel);
                textMeshComponent.gameObject.SetActive(false);
                _textComponentPool.Enqueue(textMeshComponent);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                SendChatText();
            }
        }

        private void SendChatText()
        {
            string userInputText = chatInput.text;
            if (!string.IsNullOrEmpty(userInputText))
            {
                var chatMessage = new ChatData
                {
                    type = "chat",
                    text = userInputText
                };
        
                string jsonMessage = JsonUtility.ToJson(chatMessage);
                wsHandler.SendWebSocketMessage(jsonMessage);
        
                // Display the message locally
                ShowChatText(userInputText);
        
                chatInput.text = "";
                chatInput.ActivateInputField();
            }
        }


        public void ProcessIncomingChatData(string jsonMessage)
        {
            var chatData = JsonUtility.FromJson<ChatData>(jsonMessage);
            ShowChatText(chatData.text);
        }

        private void ShowChatText(string chatText)
        {
            TextMeshProUGUI textComponent = GetOrCreateTextComponentObject();
            textComponent.gameObject.SetActive(true);
            textComponent.text = chatText;
            _activeTextComponents.Add(textComponent);

            ManageTextComponentPool();
            ScrollToBottom();
        }

        private TextMeshProUGUI GetOrCreateTextComponentObject()
        {
            if (_textComponentPool.Count > 0)
            {
                return _textComponentPool.Dequeue();
            }
            return Instantiate(chatTextPrefab, contentPanel);
        }

        private void ManageTextComponentPool()
        {
            if (_activeTextComponents.Count > poolSize)
            {
                TextMeshProUGUI oldestTextComponent = _activeTextComponents[0];
                _activeTextComponents.RemoveAt(0);
                oldestTextComponent.gameObject.SetActive(false);
                _textComponentPool.Enqueue(oldestTextComponent);
            }
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
}
