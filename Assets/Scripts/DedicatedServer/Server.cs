using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Core;
using Unity.Services.Multiplay;
using UnityEngine;
using UnityEngine.SceneManagement;

public class Server : MonoBehaviour
{
    private IServerQueryHandler serverQueryHandler;
    void Awake()
    {
        // Disable unnecessary systems for headless
        Application.runInBackground = true;
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = 60;
        
        // Disable rendering
        if (Camera.main) Camera.main.enabled = false;
        
        // Disable audio
        AudioListener.volume = 0f;
    }
    async void Start()
    {
        DontDestroyOnLoad(gameObject);
        
        // Initialize Unity Services
        await UnityServices.InitializeAsync();
        
        // Setup Network Transport
        var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        
        // Get server config from Unity Gaming Services
        var serverConfig = MultiplayService.Instance.ServerConfig;
        transport.SetConnectionData(
            serverConfig.IpAddress,
            (ushort)serverConfig.Port
        );
        
        // Start the server
        if (!NetworkManager.Singleton.StartServer())
        {
            Debug.LogError("Failed to start server");
            return;
        }

        // Setup callbacks
        NetworkManager.Singleton.OnClientConnectedCallback += HandleClientConnected;
        NetworkManager.Singleton.OnConnectionEvent += HandleConnectionEvent;
        
        // Load initial scene
        NetworkManager.Singleton.SceneManager.LoadScene("Level1", LoadSceneMode.Single);
        
        // Mark server as ready
        await MultiplayService.Instance.ReadyServerForPlayersAsync();        
        Debug.Log($"Server started on {transport.ConnectionData.Address}:{transport.ConnectionData.Port}");
    }
    
    private void HandleConnectionEvent(NetworkManager manager, ConnectionEventData data)
    {
        switch (data.EventType)
        {
            case ConnectionEvent.ClientConnected:
                Debug.Log($"Client {data.ClientId} connected");
                break;
            case ConnectionEvent.ClientDisconnected:
                Debug.Log($"Client {data.ClientId} disconnected");
                break;
        }
    }


    private void HandleClientConnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} connected");
    }

    private void HandleClientDisconnected(ulong clientId)
    {
        Debug.Log($"Client {clientId} disconnected");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnConnectionEvent -= HandleConnectionEvent;
        }
    }
}