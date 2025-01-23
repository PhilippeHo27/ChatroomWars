using System.Collections;
using System.Collections.Generic;
using Core.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Core.WebSocket;
using DG.Tweening;

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

        [SerializeField] private Button confirmNameButton;
        [SerializeField] private Button renameButton;

        private string _savedUsername = string.Empty;
        private Stack<UIState> _stateHistory = new Stack<UIState>();

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
            onlineButton.onClick.AddListener(HandleOnlineButtonClick);
            offlineButton.onClick.AddListener(HandleOfflineButtonClick);
            websocketChatButton.onClick.AddListener(() => SceneLoader.Instance.LoadScene("WebsocketChatExperiment"));
            offlineScene.onClick.AddListener(() => SceneLoader.Instance.LoadScene("OfflinePrototype"));
            backButton.onClick.AddListener(HandleBackButton);
            confirmNameButton.onClick.AddListener(HandleConfirmName);
            renameButton.onClick.AddListener(HandleRename);
            
            userInputField.onValueChanged.AddListener(newValue => usernameText.text = $"{newValue}");
            userInputField.onSubmit.AddListener(_ => HandleConfirmName());
            _savedUsername = PlayerPrefs.GetString("Username", "");
            
            //PlayerPrefs.DeleteKey("Username"); // DEBUG LINE 
            
            SaveCurrentState("Initial");
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
            SaveObjectState(state, renameButton?.gameObject);

            _stateHistory.Push(state);
        }

        private void SaveObjectState(UIState state, GameObject obj)
        {
            if (obj != null)
            {
                state.ObjectStates[obj] = obj.activeSelf;
            }
        }

        private void HandleOnlineButtonClick()
        {
            SaveCurrentState("OnlineMenu");
            
            onlineButton.gameObject.SetActive(false);
            offlineButton.gameObject.SetActive(false);
            WebSocketNetworkHandler.Instance.Connect();
            
            if (string.IsNullOrEmpty(_savedUsername))
            {
                userInputField.gameObject.SetActive(true);
                confirmNameButton.gameObject.SetActive(true);
                usernameText.gameObject.SetActive(true);
            }
            else
            {
                usernameText.text = $"Welcome back, {_savedUsername}";
                usernameText.gameObject.SetActive(true);
                renameButton.gameObject.SetActive(true);
                websocketChatButton.gameObject.SetActive(true);
                pongGameButton.gameObject.SetActive(true);
            }
            
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
                
                PlayerPrefs.SetString("Username", newUsername);
                PlayerPrefs.Save();
                
                SubmitUserNameToServer();
                
                confirmNameButton.gameObject.SetActive(false);
                userInputField.gameObject.SetActive(false);
                websocketChatButton.gameObject.SetActive(true);
                pongGameButton.gameObject.SetActive(true);
                backButton.gameObject.SetActive(true);
            }
            else
            {
                PopText("Please enter a username!");
            }
        }

        private void HandleRename()
        {
            SaveCurrentState("RenameMenu");
            
            usernameText.gameObject.SetActive(false);
            renameButton.gameObject.SetActive(false);
            websocketChatButton.gameObject.SetActive(false);
            pongGameButton.gameObject.SetActive(false);

            userInputField.text = PlayerPrefs.GetString("Username");
            userInputField.gameObject.SetActive(true);
            confirmNameButton.gameObject.SetActive(true);
            usernameText.gameObject.SetActive(true);

            userInputField.ActivateInputField();
        }

        private void HandleBackButton()
        {
            if (_stateHistory.Count <= 1)
            {
                Debug.Log("No previous state to return to");
                return;
            }

            var currentState = _stateHistory.Pop();
            var previousState = _stateHistory.Peek();
            Debug.Log($"Going back from {currentState.StateName} to {previousState.StateName}");

            foreach (var kvp in previousState.ObjectStates)
            {
                kvp.Key.SetActive(kvp.Value);
            }
        }
        
        private void SubmitUserNameToServer()
        {
            string username = PlayerPrefs.GetString("Username");
            if (!string.IsNullOrEmpty(username))
            {
                var chatMessage = new ChatData
                {
                    Type = PacketType.UserInfo,
                    Text = username
                };
                WebSocketNetworkHandler.Instance.SendWebSocketPackage(chatMessage);
            }
        }
        
        private void PopText(string message)
        {
            usernameText.text = message;
            usernameText.transform.localScale = Vector3.one;
    
            Sequence sequence = DOTween.Sequence();
            sequence.Append(usernameText.transform.DOScale(1.2f, 0.2f))
                .Append(usernameText.transform.DOScale(1f, 0.1f))
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
            renameButton.onClick.RemoveAllListeners();
            userInputField.onValueChanged.RemoveAllListeners();
            userInputField.onSubmit.RemoveAllListeners();
        }
    }
}
