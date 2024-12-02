using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

namespace Core
{
    public class NetworkHandler : IndestructibleSingletonBehaviour<NetworkHandler>
    {
        private NetworkManager _netManager;
        private UnityTransport _transport;

        [SerializeField] private string ipAddress = "127.0.0.1";
        [SerializeField] private ushort port = 7777;

        protected override void OnSingletonAwake()
        {
            _netManager = GetComponent<NetworkManager>();
            if (_netManager == null)
                _netManager = gameObject.AddComponent<NetworkManager>();

            _transport = GetComponent<UnityTransport>();
            if (_transport == null)
                _transport = gameObject.AddComponent<UnityTransport>();

            // Configure transport This section will be configured differently when we do WebGL
            _transport.ConnectionData.Address = ipAddress;
            _transport.ConnectionData.Port = port;
        }

        public void StartHost()
        {
            _netManager.StartHost();
            Debug.Log($"Started Host on {ipAddress}:{port}");
        }

        public void StartClient()
        {
            _netManager.StartClient();
            Debug.Log($"Started Client, connecting to {ipAddress}:{port}");
        }

        // Optional: Methods to change connection settings
        public void SetConnectionDetails(string ip, ushort newPort)
        {
            ipAddress = ip;
            port = newPort;
            _transport.ConnectionData.Address = ipAddress;
            _transport.ConnectionData.Port = port;
        }
    }
}