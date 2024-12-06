// using System.Collections.Generic;
// using System.Threading.Tasks;
// using Unity.Collections;
// using Unity.Netcode;
// using Unity.Services.CloudCode;
// using UnityEngine;
//
// public class Player : NetworkBehaviour
// {
//     public NetworkVariable<FixedString32Bytes> PlayerId;
//     public NetworkVariable<FixedString32Bytes> CharacterName;
//     public NetworkVariable<FixedString32Bytes> PlayerName;
//
//     void Awake()
//     {
//         PlayerId = new NetworkVariable<FixedString32Bytes>();
//         PlayerName = new NetworkVariable<FixedString32Bytes>();
//         CharacterName = new NetworkVariable<FixedString32Bytes>();
//     }
//
//     public override void OnNetworkSpawn()
//     {
//         if (IsServer)
//         {
//             PlayerId.Value = ServerPlayerManager.GetPlayerId(OwnerClientId);
//             PlayerName.Value = ServerPlayerManager.GetPlayerName(OwnerClientId);
//             CharacterName.Value = ServerPlayerManager.GetCharacterName(OwnerClientId);
//
//             StartCoroutine(LoadCharacterAsync());
//         }
//         gameObject.name = "Player " + PlayerName.Value + " Character " + CharacterName.Value;
//     }
//
//     async Awaitable LoadCharacterAsync()
//     {
//         var persistedCharacterData = await LoadCharacterOnServer(PlayerId.Value.Value, CharacterName.Value.Value);
//         Debug.Log(persistedCharacterData.Name + " " + persistedCharacterData.Class);
//     }
//
//     public async Task<PersistedCharacterData> LoadCharacterOnServer(string playerId, string characterName)
//     {
//         if (string.IsNullOrWhiteSpace(characterName))
//         {
//             Debug.LogError("Can't Load empty character");
//             return null;
//         }
//
//         Debug.LogWarning("Loading Character on server for playerid " + playerId + " and character " +
//                          characterName);
//         
//         var result = await CloudCodeService.Instance.CallModuleEndpointAsync<PersistedCharacterData>(
//             "ExtractCloud",
//             "LoadCharacterOnServer",
//             new Dictionary<string, object>
//             {
//                 {"playerId", playerId},
//                 {"characterName", characterName}
//             });
//        
//         return result;
//     }
// }
// [CloudCodeFunction("LoadCharacterOnServer")]
//     public async Task<PersistedCharacterData> LoadCharacterOnServer(IExecutionContext ctx, IGameApiClient gameApiClient, string playerId, string characterName)
//     {
//         var response = await gameApiClient.CloudSaveData.GetProtectedItemsAsync(
//             ctx,
//             ctx.ServiceToken,
//             ctx.ProjectId,
//             playerId,
//             new List<string>() { characterName });
//
//         if (response.Data.Results.Count == 0)
//             throw new Exception($"No player returned searching player {playerId} for {characterName}");
//
//         var json = response.Data.Results[0].Value.ToString();
//
//         var character = JsonConvert.DeserializeObject<PersistedCharacterData>(json);
//         if (character == default)