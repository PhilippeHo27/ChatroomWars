using System;
using System.Collections;
using System.Threading.Tasks;
using Core.Singletons;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Core.WebSocket;
using Random = UnityEngine.Random;

namespace Core
{
    public class IntroCanvas : MonoBehaviour
    {
        [Header("Canvas Groups")]
        [SerializeField] private CanvasGroup welcomeScreenGroup;
        [SerializeField] private CanvasGroup onlineScreenGroup;
        [SerializeField] private CanvasGroup offlineScreenGroup;
        
        [Header("Welcome Screen Elements")]
        [SerializeField] private Button onlineButton;
        [SerializeField] private Button offlineButton;
        
        [Header("Online Screen Elements")]
        [SerializeField] private TMP_InputField userInputField;
        [SerializeField] private Image usernameTextBackground;
        [SerializeField] private TMP_Text usernameText;
        [SerializeField] private TMP_Text errorText;
        [SerializeField] private Button confirmNameButton;
        [SerializeField] private Button randomizeNameButton;
        [SerializeField] private Button websocketChatButton;
        //[SerializeField] private Button pongGameButton;
        [SerializeField] private Button vinceGameButton;
        [SerializeField] private TMP_InputField[] userInputParameter;

        [Header("Offline Screen Elements")]
        [SerializeField] private Button offlineScene;
        [SerializeField] private Button offlineVinceGame;
        [SerializeField] private Toggle offlineBlindToggle;
        
        [Header("Navigation")]
        [SerializeField] private Button backButton;
        
        private string _savedUsername = string.Empty;

        private void Start()
        {
            SetupButtonListeners();
            _savedUsername = PlayerPrefs.GetString("Username", "");
            ShowWelcomeScreen();
            
            for (int i = 0; i < userInputParameter.Length; i++)
            {
                int index = i;
                userInputParameter[i].contentType = TMP_InputField.ContentType.IntegerNumber;
                userInputParameter[i].onEndEdit.AddListener(value => OnParameterChanged(value, index));
                //Debug.Log($"Input field {i} name: {userInputParameter[i].name}");
            }
        }

        private void SetupButtonListeners()
        {
            onlineButton.onClick.AddListener(ShowOnlineScreen);
            offlineButton.onClick.AddListener(ShowOfflineScreen);
            backButton.onClick.AddListener(HandleBackButton);
            confirmNameButton.onClick.AddListener(HandleConfirmName);
            randomizeNameButton.onClick.AddListener(RandomizeName);
            
            websocketChatButton.onClick.AddListener(() => _ = ConnectToWebsocketSafe("ChatRoom"));
            vinceGameButton.onClick.AddListener(() => _ = ConnectToWebsocketSafe("HiddenGame"));
            offlineScene.onClick.AddListener(() => SceneLoader.Instance.LoadScene("OfflinePrototype"));
            offlineVinceGame.onClick.AddListener(LoadVinceOfflineGame);
            
            userInputField.onValueChanged.AddListener(newValue => usernameText.text = $"Username: {newValue}");
            userInputField.onSubmit.AddListener(_ => HandleConfirmName());
        }
        
        private void ShowWelcomeScreen()
        {
            SetCanvasGroupActive(welcomeScreenGroup, true);
            SetCanvasGroupActive(onlineScreenGroup, false);
            SetCanvasGroupActive(offlineScreenGroup, false);
            backButton.gameObject.SetActive(false);
        }
        
        private void ShowOnlineScreen()
        {
            SetCanvasGroupActive(welcomeScreenGroup, false);
            SetCanvasGroupActive(onlineScreenGroup, true);
            SetCanvasGroupActive(offlineScreenGroup, false);
            backButton.gameObject.SetActive(true);
            
            // Show the name input section
            userInputField.text = _savedUsername;
            userInputField.gameObject.SetActive(true);
            confirmNameButton.gameObject.SetActive(true);
            randomizeNameButton.gameObject.SetActive(true);
            usernameTextBackground.gameObject.SetActive(true);
            usernameText.gameObject.SetActive(true);
            
            // Hide game options until name is confirmed
            websocketChatButton.gameObject.SetActive(false);
            //pongGameButton.gameObject.SetActive(false);
            vinceGameButton.gameObject.SetActive(false);
        }
        
        private void ShowOfflineScreen()
        {
            SetCanvasGroupActive(welcomeScreenGroup, false);
            SetCanvasGroupActive(onlineScreenGroup, false);
            SetCanvasGroupActive(offlineScreenGroup, true);
            backButton.gameObject.SetActive(true);
        }
        
        private void HandleBackButton()
        {
            errorText.text = "";
    
            offlineBlindToggle.isOn = true;
            
            userInputField.gameObject.SetActive(true);
            confirmNameButton.gameObject.SetActive(true);
            randomizeNameButton.gameObject.SetActive(true);
            
            websocketChatButton.gameObject.SetActive(false);
            //pongGameButton.gameObject.SetActive(false);
            vinceGameButton.gameObject.SetActive(false);
    
            ShowWelcomeScreen();
        }
        
        private void HandleConfirmName()
        {
            string newUsername = userInputField.text.Trim();
            if (!string.IsNullOrEmpty(newUsername))
            {
                _savedUsername = newUsername;
                PlayerPrefs.SetString("Username", newUsername);
                PlayerPrefs.Save();
                
                // Hide name input section
                confirmNameButton.gameObject.SetActive(false);
                userInputField.gameObject.SetActive(false);
                randomizeNameButton.gameObject.SetActive(false);

                
                // Show game options
                usernameText.text = $"Username: {newUsername}";
                websocketChatButton.gameObject.SetActive(true);
                //pongGameButton.gameObject.SetActive(true);
                vinceGameButton.gameObject.SetActive(true);
            }
            else
            {
                GameManager.Instance.TextAnimations.PopText(errorText, "Please enter a username!");
            }
        }

        private void RandomizeName()
        {
            string[] names = new string[]
            {
                // 10 chatroom
                "CodeJunkie", "ByteMaster", "ScriptLord", "DataMiner", "NetSurfer", 
                "LogicBolt", "ThreadWeaver", "BinaryBard", "AlgorithmGuy", "SyntaxHero",
                // 10 gamers
                "ApexPredator", "BlazeRunner", "CipherBlade", "DuskFang", "EchoStrike",
                "FrostNova", "GhostHunter", "HavocAgent", "InfernoZero", "JoltShock",
                // 10 wow
                "IronhideMarauder", "Stonebreaker", "GrimAxe", "Thunderhoof", "Bloodfist",
                "VoidCaller", "ShadowCrawler", "Frostbinder", "Earthshaker", "Stormbringer",
                // 10 Normies
                "Henry", "Sarah", "Michael", "Jessica", "David",
                "Emily", "James", "Ashley", "Robert", "Megan",
                // 10 Futuristic
                "CommanderShepard", "TaliZorah", "GarrusVakarian", "LiaraTsoni", "Wrex",
                "Grunt", "MirandaLawson", "JacobTaylor", "MordinSolus", "EDI" 
            };


            string randomName = names[Random.Range(0, names.Length)];
    
            userInputField.text = randomName;
            usernameText.text = $"Username: {randomName}";
        }

        private void SetCanvasGroupActive(CanvasGroup group, bool active)
        {
            group.alpha = active ? 1 : 0;
            group.interactable = active;
            group.blocksRaycasts = active;
        }
        
        // private void ConnectToWebsocket(string sceneName)
        // {
        //     if (WebSocketNetworkHandler.Instance.IsConnected || WebSocketNetworkHandler.Instance.IsConnecting)
        //     {
        //         //Debug.Log("Already connected or connecting. Ignoring connection request.");
        //         return;
        //     }
        //
        //     GameManager.Instance.isOnline = true;
        //     GameManager.Instance.blindModeActive = true;
        //     GameManager.Instance.playingAgainstAI = false;
        //
        //     // Remove the duplicate Connect() call here
        //     StartCoroutine(ConnectWithRetries(sceneName, 3));
        // }
        
        // private async void ConnectToWebsocket(string sceneName)
        // {
        //     if (WebSocketNetworkHandler.Instance.IsConnected || WebSocketNetworkHandler.Instance.IsConnecting)
        //     {
        //         return;
        //     }
        //
        //     GameManager.Instance.isOnline = true;
        //     GameManager.Instance.blindModeActive = true;
        //     GameManager.Instance.playingAgainstAI = false;
        //
        //     bool success = await ConnectWithRetries(sceneName, 3);
        // }
        //
        
        private async Task ConnectToWebsocketSafe(string sceneName)
        {
            try
            {
                if (WebSocketNetworkHandler.Instance.IsConnected || WebSocketNetworkHandler.Instance.IsConnecting)
                {
                    return;
                }

                // This was missing! Original setup logic:
                GameManager.Instance.isOnline = true;
                GameManager.Instance.blindModeActive = true;
                GameManager.Instance.playingAgainstAI = false;

                bool success = await ConnectWithRetries(sceneName, 3);
                if (!success)
                {
                    // Handle connection failure UI updates here
                }
            }
            catch (Exception ex)
            {
                Debug.LogError($"Connection failed: {ex.Message}");
                GameManager.Instance.TextAnimations.PopText(errorText, "Connection failed!");
            }
        }
        
        private async Task<bool> ConnectWithRetries(string sceneName, int maxRetries = 2)
        {
            int retryCount = 0;
            bool connected = false;

            while (!connected && retryCount <= maxRetries)
            {
                errorText.text = retryCount > 0 ? $"Retrying connection ({retryCount}/{maxRetries})..." : "Connecting...";

                WebSocketNetworkHandler.Instance.Connect();

                // Wait for connection with timeout
                var timeoutDuration = TimeSpan.FromSeconds(3.0f);
                var startTime = DateTime.Now;

                while (!WebSocketNetworkHandler.Instance.IsConnected && DateTime.Now - startTime < timeoutDuration)
                {
                    await Task.Delay(200);
                }

                connected = WebSocketNetworkHandler.Instance.IsConnected;

                if (!connected)
                {
                    retryCount++;
                    await Task.Delay(500);
                }
            }

            // Early return for connection failure
            if (!connected)
            {
                GameManager.Instance.TextAnimations.PopText(errorText, "Failed to connect to server after multiple attempts!");
                return false;
            }

            // Send username and wait for confirmation
            bool usernameAccepted = await WebSocketNetworkHandler.Instance.SendPacketReliable(
                new StringPacket { Type = PacketType.UserInfo, Text = _savedUsername }
            );

            if (!usernameAccepted)
            {
                Debug.LogError("Server rejected username or timed out");
                return false;
            }

            Debug.Log("Username accepted by server");
    
            // Join lobby and wait for confirmation
            bool lobbyJoined = await WebSocketNetworkHandler.Instance.SendPacketReliable(
                new StringPacket { Type = PacketType.RoomJoin, Text = "lobbyRoom" }
            );
    
            if (!lobbyJoined)
            {
                Debug.LogError("Failed to join lobby");
                return false;
            }

            SceneLoader.Instance.LoadScene(sceneName);
            return true;
        }



        // private IEnumerator ConnectWithRetries(string sceneName, int maxRetries = 2)
        // {
        //     int retryCount = 0;
        //     bool connected = false;
        //
        //     while (!connected && retryCount <= maxRetries)
        //     {
        //         errorText.text = retryCount > 0 ? $"Retrying connection ({retryCount}/{maxRetries})..." : "Connecting...";
        //
        //         WebSocketNetworkHandler.Instance.Connect();
        //
        //         // Wait for connection with timeout
        //         float timeoutDuration = 3.0f;
        //         float elapsed = 0;
        //
        //         while (!WebSocketNetworkHandler.Instance.IsConnected && elapsed < timeoutDuration)
        //         {
        //             yield return new WaitForSeconds(0.2f);
        //             elapsed += 0.2f;
        //         }
        //
        //         connected = WebSocketNetworkHandler.Instance.IsConnected;
        //
        //         if (!connected)
        //         {
        //             retryCount++;
        //             yield return new WaitForSeconds(0.5f); // Brief pause between retries
        //         }
        //     }
        //
        //     if (connected)
        //     {
        //         // Connection successful
        //         var chatMessage = new StringPacket
        //         {
        //             Type = PacketType.UserInfo,
        //             Text = _savedUsername
        //         };
        //         WebSocketNetworkHandler.Instance.SendPacket(chatMessage);
        //         bool usernameAccepted = await WebSocketNetworkHandler.Instance.SendUserInfoAndWaitForConfirmation(_savedUsername);
        //         
        //         if (usernameAccepted)
        //         {
        //             Debug.Log("Username accepted by server");
        //             SceneLoader.Instance.LoadScene(sceneName);
        //         }
        //         else
        //         {
        //             Debug.LogError("Server rejected username or timed out");
        //             // Show error to user, don't proceed
        //         }
        //         
        //         yield return new WaitForSeconds(0.1f);
        //
        //         // Auto-join general lobby
        //         var joinLobbyMessage = new StringPacket
        //         {
        //             Type = PacketType.RoomJoin,
        //             Text = "lobbyRoom"  // Your permanent room
        //         };
        //         WebSocketNetworkHandler.Instance.SendPacket(joinLobbyMessage);
        //         
        //
        //         yield return new WaitForSeconds(0.2f);
        //         SceneLoader.Instance.LoadScene(sceneName);
        //         
        //     }
        //     else
        //     {
        //         GameManager.Instance.TextAnimations.PopText(errorText, "Failed to connect to server after multiple attempts!");
        //     }
        // }

        private void LoadVinceOfflineGame()
        {
            GameManager.Instance.playingAgainstAI = true;
            GameManager.Instance.isOnline = false;
            GameManager.Instance.blindModeActive = offlineBlindToggle.isOn;
            SceneLoader.Instance.LoadScene("HiddenGame");
        }
        
        private void OnParameterChanged(string value, int index)
        {
            if (string.IsNullOrEmpty(value)) return;
    
            if (float.TryParse(value, out float inputValue))
            {
                switch (index)
                {
                    case 0:
                        GameManager.Instance.numberOfRounds = (byte)inputValue;
                        break;
                    case 1:
                        GameManager.Instance.timer = inputValue;
                        break;

                }
            }
        }

        private void OnDestroy()
        {
            onlineButton.onClick.RemoveAllListeners();
            offlineButton.onClick.RemoveAllListeners();
            websocketChatButton.onClick.RemoveAllListeners();
            //pongGameButton.onClick.RemoveAllListeners();
            vinceGameButton.onClick.RemoveAllListeners();
            offlineScene.onClick.RemoveAllListeners();
            offlineVinceGame.onClick.RemoveAllListeners();
            backButton.onClick.RemoveAllListeners();
            confirmNameButton.onClick.RemoveAllListeners();
            randomizeNameButton.onClick.RemoveAllListeners();
            userInputField.onValueChanged.RemoveAllListeners();
            userInputField.onSubmit.RemoveAllListeners();
        }
    }
}
