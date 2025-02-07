using System.Collections;
using Core.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.WebSocket;
using DG.Tweening;
using System.Collections.Generic;

namespace Core
{
    public class IntroCanvas : MonoBehaviour
    {
        [SerializeField] private Button onlineButton;
        [SerializeField] private Button offlineButton;
        [SerializeField] private Button websocketChatButton;
        [SerializeField] private Button pongGameButton;
        [SerializeField] private Button offlineScene;
        [SerializeField] private Button backButton;
        
        [SerializeField] private TMP_InputField userInputField;
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button confirmNameButton;

        private string _savedUsername = string.Empty;
        private readonly Stack<UIState> _stateHistory = new Stack<UIState>();

        private class UIState
        {
            public Dictionary<GameObject, bool> ObjectStates { get; }
            public string StateName { get; }

            public UIState(string name)
            {
                ObjectStates = new Dictionary<GameObject, bool>();
                StateName = name;
            }
        }

        private void Start()
        {
            SetupButtonListeners();
            _savedUsername = PlayerPrefs.GetString("Username", "");
            SaveCurrentState("Initial");
        }

        private void SetupButtonListeners()
        {
            onlineButton.onClick.AddListener(HandleOnlineButtonClick);
            offlineButton.onClick.AddListener(HandleOfflineButtonClick);
            backButton.onClick.AddListener(HandleBackButton);
            confirmNameButton.onClick.AddListener(HandleConfirmName);
            
            websocketChatButton.onClick.AddListener(HandleWebsocketChatClick);
            pongGameButton.onClick.AddListener(HandlePongGameClick);
            offlineScene.onClick.AddListener(() => SceneLoader.Instance.LoadScene("OfflinePrototype"));

            
            userInputField.onValueChanged.AddListener(newValue => usernameText.text = $"Username: {newValue}");
            userInputField.onSubmit.AddListener(_ => HandleConfirmName());
        }
        
        private void HandleOnlineButtonClick()
        {
            SaveCurrentState("OnlineMenu");
            
            onlineButton.gameObject.SetActive(false);
            offlineButton.gameObject.SetActive(false);
            
            userInputField.text = _savedUsername;
            userInputField.gameObject.SetActive(true);
            confirmNameButton.gameObject.SetActive(true);
            usernameText.gameObject.SetActive(true);
            backButton.gameObject.SetActive(true);
        }

        private void HandleOfflineButtonClick()
        {
            SaveCurrentState("OfflineMenu");
            onlineButton.gameObject.SetActive(false);
            offlineButton.gameObject.SetActive(false);
            offlineScene.gameObject.SetActive(true);
            backButton.gameObject.SetActive(true);
        }

        private void HandleConfirmName()
        {
            string newUsername = userInputField.text.Trim();
            if (!string.IsNullOrEmpty(newUsername))
            {
                SaveCurrentState("PostNameConfirm");
                _savedUsername = newUsername;
                PlayerPrefs.SetString("Username", newUsername);
                PlayerPrefs.Save();
                
                confirmNameButton.gameObject.SetActive(false);
                userInputField.gameObject.SetActive(false);
                usernameText.text = $"Username: {newUsername}";
                websocketChatButton.gameObject.SetActive(true);
                pongGameButton.gameObject.SetActive(true);
            }
            else
            {
                PopText(errorText, "Please enter a username!");
            }
        }

        private void HandleBackButton()
        {
            if (_stateHistory.Count <= 1) return;

            _stateHistory.Pop();
            var previousState = _stateHistory.Peek();

            foreach (var kvp in previousState.ObjectStates)
            {
                kvp.Key.SetActive(kvp.Value);
            }
        }

        private void SaveCurrentState(string stateName)
        {
            var state = new UIState(stateName);
            SaveObjectState(state, onlineButton?.gameObject);
            SaveObjectState(state, offlineButton?.gameObject);
            SaveObjectState(state, websocketChatButton?.gameObject);
            SaveObjectState(state, pongGameButton?.gameObject);
            SaveObjectState(state, offlineScene?.gameObject);
            SaveObjectState(state, backButton?.gameObject);
            SaveObjectState(state, userInputField?.gameObject);
            SaveObjectState(state, usernameText?.gameObject);
            SaveObjectState(state, confirmNameButton?.gameObject);

            _stateHistory.Push(state);
        }

        private void SaveObjectState(UIState state, GameObject obj)
        {
            if (obj != null) state.ObjectStates[obj] = obj.activeSelf;
        }
        
        private void HandleWebsocketChatClick()
        {
            WebSocketNetworkHandler.Instance.Connect();
            StartCoroutine(CheckConnectionAndLoad("WebsocketChatExperiment"));
        }

        private void HandlePongGameClick()
        {
            WebSocketNetworkHandler.Instance.Connect();
            StartCoroutine(CheckConnectionAndLoad("Pong"));

        }

        private IEnumerator CheckConnectionAndLoad(string sceneName)
        {
            errorText.text = "Connecting...";
            yield return new WaitForSeconds(0.3f);

            if (WebSocketNetworkHandler.Instance.IsConnected)
            {
                // Send username before changing scene
                var chatMessage = new ChatData
                {
                    Type = PacketType.UserInfo,
                    Text = _savedUsername
                };
                WebSocketNetworkHandler.Instance.SendWebSocketPackage(chatMessage);
        
                // Give a small delay for the server to process
                yield return new WaitForSeconds(0.2f);
        
                // Then load the scene
                SceneLoader.Instance.LoadScene(sceneName);
            }
            else
            {
                PopText(errorText, "Failed to connect to server!");
            }
        }

        
        private void PopText(TMP_Text tmpText, string message)
        {
            tmpText.text = message;
            tmpText.transform.localScale = Vector3.one;
    
            Sequence sequence = DOTween.Sequence();
            sequence.Append(tmpText.transform.DOScale(1.2f, 0.2f))
                .Append(tmpText.transform.DOScale(1f, 0.1f))
                .SetEase(Ease.OutBack);
        }

        private void OnDestroy()
        {
            onlineButton.onClick.RemoveAllListeners();
            offlineButton.onClick.RemoveAllListeners();
            websocketChatButton.onClick.RemoveAllListeners();
            offlineScene.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
            confirmNameButton.onClick.RemoveAllListeners();
            userInputField.onValueChanged.RemoveAllListeners();
            userInputField.onSubmit.RemoveAllListeners();
        }
    }
}
