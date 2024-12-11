using Unity.Netcode;
using UnityEngine;

#if !UNITY_WEBGL
using Unity.Services.Multiplay;
using Unity.Services.Core;
using System.Threading.Tasks;
#endif

/// <summary>
/// An example of how to use SQP from the server using the Multiplay SDK.
/// The ServerQueryHandler reports the given information to the Multiplay Service.
/// </summary>
public class Example_ServerQueryHandler : MonoBehaviour
{
#if !UNITY_WEBGL
    const ushort k_DefaultMaxPlayers = 10;
    const string k_DefaultServerName = "MyServerExample";
    const string k_DefaultGameType = "MyGameType";
    const string k_DefaultBuildId = "test2";
    const string k_DefaultMap = "MyMap";

    IServerQueryHandler m_ServerQueryHandler;

    async void Start()
    {
        // Initialize Unity Services
        await UnityServices.InitializeAsync();

        // Wait for MultiplayService to be available
        while (MultiplayService.Instance == null)
        {
            await Task.Delay(100);
        }

        try
        {
            m_ServerQueryHandler = await MultiplayService.Instance.StartServerQueryHandlerAsync(
                k_DefaultMaxPlayers, 
                k_DefaultServerName, 
                k_DefaultGameType, 
                k_DefaultBuildId, 
                k_DefaultMap);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to start Server Query Handler: {e.Message}");
        }
    }

    void Update()
    {
        if (m_ServerQueryHandler != null)
        {
            if (NetworkManager.Singleton != null && 
                NetworkManager.Singleton.ConnectedClients.Count != m_ServerQueryHandler.CurrentPlayers)
            {
                m_ServerQueryHandler.CurrentPlayers = (ushort)NetworkManager.Singleton.ConnectedClients.Count;
            }

            m_ServerQueryHandler.UpdateServerCheck();
        }
    }

    public void ChangeQueryResponseValues(ushort maxPlayers, string serverName, string gameType, string buildId)
    {
        if (m_ServerQueryHandler != null)
        {
            m_ServerQueryHandler.MaxPlayers = maxPlayers;
            m_ServerQueryHandler.ServerName = serverName;
            m_ServerQueryHandler.GameType = gameType;
            m_ServerQueryHandler.BuildId = buildId;
        }
    }

    public void PlayerCountChanged(ushort newPlayerCount)
    {
        if (m_ServerQueryHandler != null)
        {
            m_ServerQueryHandler.CurrentPlayers = newPlayerCount;
        }
    }
#endif
}
