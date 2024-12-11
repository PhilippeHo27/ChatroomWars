using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.SceneManagement;

#if !UNITY_WEBGL
using Unity.Services.Core;
using Unity.Services.Multiplay;
#endif

public class Server : MonoBehaviour
{
    #if !UNITY_WEBGL
    private IServerQueryHandler serverQueryHandler;
    #endif

    void Awake()
    {
        #if UNITY_SERVER || UNITY_DEDICATED_SERVER
            // Server-specific optimizations
            Application.runInBackground = true;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Screen.SetResolution(1, 1, FullScreenMode.Windowed);
            Screen.fullScreen = false;

            // Aggressive graphics disabling
            QualitySettings.shadowDistance = 0;
            QualitySettings.shadows = ShadowQuality.Disable;
            QualitySettings.antiAliasing = 0;
            QualitySettings.lodBias = 0;
            
            // Disable all rendering-related systems
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (var cam in cameras)
            {
                cam.enabled = false;
                Destroy(cam);
            }
            
            // Disable all renderers
            Renderer[] renderers = FindObjectsOfType<Renderer>();
            foreach (var renderer in renderers)
            {
                renderer.enabled = false;
                Destroy(renderer);
            }
            
            // Disable all UI elements
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (var canvas in canvases)
            {
                canvas.enabled = false;
                Destroy(canvas);
            }
            
            // Disable audio
            AudioListener[] listeners = FindObjectsOfType<AudioListener>();
            foreach (var listener in listeners)
            {
                listener.enabled = false;
                Destroy(listener);
            }
            AudioListener.volume = 0f;
            AudioListener.pause = true;
        #endif
    }

    async void Start()
    {
        #if UNITY_SERVER || UNITY_DEDICATED_SERVER
            DontDestroyOnLoad(gameObject);
            
            try
            {
                #if !UNITY_WEBGL
                await UnityServices.InitializeAsync();
                
                var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
                if (transport == null)
                {
                    Debug.LogError("Failed to get UnityTransport component");
                    return;
                }

                var serverConfig = MultiplayService.Instance.ServerConfig;
                transport.SetConnectionData(
                    serverConfig.IpAddress,
                    (ushort)serverConfig.Port
                );
                #endif

                if (!NetworkManager.Singleton.StartServer())
                {
                    Debug.LogError("Failed to start server");
                    return;
                }

                SetupNetworkCallbacks();
                
                // Load server-specific scene version
                await LoadServerScene();
                
                #if !UNITY_WEBGL
                await MultiplayService.Instance.ReadyServerForPlayersAsync();
                Debug.Log($"Server started on {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
                #endif
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Server initialization failed: {e.Message}");
            }
        #endif
    }

    private async System.Threading.Tasks.Task LoadServerScene()
    {
        #if UNITY_SERVER || UNITY_DEDICATED_SERVER
            // Load a minimal server-only scene
            await NetworkManager.Singleton.SceneManager.LoadScene("Level1_ServerOnly", LoadSceneMode.Single).Task;
        #endif
    }

    private void SetupNetworkCallbacks()
    {
        #if UNITY_SERVER || UNITY_DEDICATED_SERVER
            NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
            NetworkManager.Singleton.OnConnectionEvent += HandleConnectionEvent;
        #endif
    }

    private void HandleConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        #if UNITY_SERVER || UNITY_DEDICATED_SERVER
            switch (data.EventType)
            {
                case ConnectionEvent.ClientConnected:
                    Debug.Log($"Client {data.ClientId} connected");
                    break;
                case ConnectionEvent.ClientDisconnected:
                    Debug.Log($"Client {data.ClientId} disconnected");
                    break;
            }
        #endif
    }

    private void HandleClientConnected(ulong clientId)
    {
        #if UNITY_SERVER || UNITY_DEDICATED_SERVER
            Debug.Log($"Client {clientId} connected");
        #endif
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        #if UNITY_SERVER || UNITY_DEDICATED_SERVER
            Debug.Log($"Client {clientId} disconnected");
        #endif
    }

    private void OnDestroy()
    {
        #if UNITY_SERVER || UNITY_DEDICATED_SERVER
            if (NetworkManager.Singleton != null)
            {
                NetworkManager.Singleton.OnConnectionEvent -= HandleConnectionEvent;
            }
        #endif
    }
}
