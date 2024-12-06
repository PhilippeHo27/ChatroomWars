using System;
using Unity.Netcode;
using UnityEngine;

public class GamePlayer : NetworkBehaviour
{
    [ContextMenu("Send Test")]
    public void SendTest()
    {
        Debug.Log("Sending Test");
        TestServerRpc();
    }

    [ServerRpc]
    void TestServerRpc()
    {
        TestClientRpc();
    }

    [ClientRpc]
    void TestClientRpc()
    {
        Debug.Log("Got Client Rpc");
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();
        if (IsLocalPlayer)
        {
            Debug.Log("I am the local player");
        }
    }

    void Start()
    {
        if (IsClient && IsLocalPlayer)
        {
            Debug.Log("Sending server rpc");
            TestServerRpc();
        }
    }
}