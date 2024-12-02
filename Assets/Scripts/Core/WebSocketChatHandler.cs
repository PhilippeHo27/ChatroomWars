using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Core
{
    public class WebSocketChatHandler : MonoBehaviour
    {
        [SerializeField] private WebSocketNetworkHandler wsHandler;
        [SerializeField] private TMP_InputField chatInput;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private RectTransform contentPanel;
        [SerializeField] private TextMeshProUGUI messageTextPrefab;
        [SerializeField] private int poolSize = 100;

        private Queue<TextMeshProUGUI> _messagePool;
        private List<TextMeshProUGUI> _activeMessages;

        private void Start()
        {
            _messagePool = new Queue<TextMeshProUGUI>();
            _activeMessages = new List<TextMeshProUGUI>();
        
            for (int i = 0; i < poolSize; i++)
            {
                TextMeshProUGUI message = Instantiate(messageTextPrefab, contentPanel);
                message.gameObject.SetActive(false);
                _messagePool.Enqueue(message);
            }
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Return))
            {
                string message = chatInput.text;
                if (!string.IsNullOrEmpty(message))
                {
                    wsHandler.SendWebSocketMessage(message);
                    chatInput.text = "";
                    chatInput.ActivateInputField();
                }
            }
        }

        public void DisplayMessage(string message)
        {
            TextMeshProUGUI messageObject;
            if (_messagePool.Count > 0)
            {
                messageObject = _messagePool.Dequeue();
            }
            else
            {
                messageObject = Instantiate(messageTextPrefab, contentPanel);
            }

            messageObject.gameObject.SetActive(true);
            messageObject.text = message;
            _activeMessages.Add(messageObject);

            if (_activeMessages.Count > poolSize)
            {
                TextMeshProUGUI oldestMessage = _activeMessages[0];
                _activeMessages.RemoveAt(0);
                oldestMessage.gameObject.SetActive(false);
                _messagePool.Enqueue(oldestMessage);
            }

            ScrollToBottom();
        }

        private void ScrollToBottom()
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.normalizedPosition = new Vector2(0, 0);
        }
    }
}