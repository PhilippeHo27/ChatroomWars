using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

#if !UNITY_WEBGL
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Lobbies;
#endif

public class Client : MonoBehaviour
{
    private async void Start()
    {
        try
        {
            #if !UNITY_WEBGL
            // Initialize Unity Services
            await UnityServices.InitializeAsync();
            
            // Sign in anonymously
            if (!AuthenticationService.Instance.IsSignedIn)
            {
                await AuthenticationService.Instance.SignInAnonymouslyAsync();
                Debug.Log($"Signed in. Player ID: {AuthenticationService.Instance.PlayerId}");
            }
            #endif

            var transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
            
            #if UNITY_WEBGL
            // WebGL specific connection settings
            transport.SetConnectionData("127.0.0.1", (ushort)7777); // Or your WebGL specific host
            #else
            // Regular connection settings
            transport.SetConnectionData("0.0.0.0", (ushort)7777); // Default testing port
            #endif

            // Setup connection callbacks
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnConnectionEvent += HandleConnectionEvent;

            // Start client
            if (NetworkManager.Singleton.StartClient())
            {
                Debug.Log("Client started successfully");
            }
            else
            {
                Debug.LogError("Failed to start client");
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to initialize client: {e.Message}");
        }
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

    private void OnClientConnected(ulong clientId)
    {
        Debug.Log($"Connected to server with clientId: {clientId}");
    }

    private void OnClientDisconnected(ulong clientId)
    {
        Debug.Log($"Disconnected from server");
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.OnConnectionEvent -= HandleConnectionEvent;
        }
    }
}
