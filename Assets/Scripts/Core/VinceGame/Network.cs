using System.Collections.Generic;
using UnityEngine;
using Core.WebSocket;
using MessagePack;

namespace Core.VinceGame
{
    public class Network
    {
        private WebSocketNetworkHandler _wsHandler;
        private GamePrototype _gameRef;
        private GameGUI _gameGUI;

        // Events
        //public System.Action<bool> OnReadyStateReceived;
        //public System.Action<int, string> OnMoveReceived;
        //public System.Action<List<int>, List<string>> OnMultipleMovesReceived;
        //public System.Action<byte[]> OnImmuneStatusReceived;

        public Network(GamePrototype gameReference, GameGUI gameGUI, WebSocketNetworkHandler wsHandler)
        {
            _gameRef = gameReference;
            _gameGUI = gameGUI;
            _wsHandler = wsHandler;
            
            // Subscribe to WebSocket events
            _wsHandler.OnGameStartConfirmation += ReceiveReadyState;
            _wsHandler.Matchmaking.OnMatchFound += MatchFound;
        }

        public void Cleanup()
        {
            // Unsubscribe from WebSocket events
            _wsHandler.OnGameStartConfirmation -= ReceiveReadyState;
            _wsHandler.Matchmaking.OnMatchFound -= MatchFound;
        }

        #region Send Methods

        public void SendReady(bool isReady)
        {
            var isReadyPacket = new BooleanPacket
            {
                Type = PacketType.VinceGameConfirmStart,
                Response = isReady
            };
            _wsHandler.SendWebSocketPackage(isReadyPacket);
        }

        public void SendMove(int index, string colorBeforeResolution)
        {
            var createData = new VinceGameData
            {
                Type = PacketType.VinceGamePacket,
                Index = (byte)index,
                SquareColor = colorBeforeResolution
            };
            
            _wsHandler.SendWebSocketPackage(createData);
        }
        
        public void SendMoves(List<int> indices, List<string> colors)
        {
            byte[] indicesArray = new byte[indices.Count];
            for (int i = 0; i < indices.Count; i++)
            {
                indicesArray[i] = (byte)indices[i];
            }
    
            var createData = new ExtraTurnPacket
            {
                Type = PacketType.ExtraTurnPacket,
                Indices = indicesArray,
                Colors = colors.ToArray()
            };
    
            _wsHandler.SendWebSocketPackage(createData);
        }

        public void SendImmuneStatus(byte index)
        {
            SendImmuneStatus(new [] { index });
        }

        public void SendImmuneStatus(byte[] indexes)
        {
            var immunePacket = new GridGameIndices 
            {
                Type = PacketType.VinceGameImmune,
                Index = indexes
            };
            _wsHandler.SendWebSocketPackage(immunePacket);
        }


        #endregion

        #region Receive Methods

        public void ReceiveMove(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<VinceGameData>(messagePackData);
            if (receivedData.SenderId != _wsHandler.ClientId)
            {
                int index = receivedData.Index;
                // OnMoveReceived?.Invoke(index, receivedData.SquareColor);
                _gameRef.HandleMoveReceived(index, receivedData.SquareColor);
            }
        }

        public void ReceiveMoves(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<ExtraTurnPacket>(messagePackData);
            if (receivedData.SenderId != _wsHandler.ClientId)
            {
                List<int> indices = new List<int>();
                List<string> colors = new List<string>();
                
                for (int i = 0; i < receivedData.Indices.Length; i++)
                {
                    indices.Add(receivedData.Indices[i]);
                    colors.Add(receivedData.Colors[i]);
                }
                
                // OnMultipleMovesReceived?.Invoke(indices, colors);
                _gameRef.HandleMultipleMovesReceived(indices, colors);
            }
        }

        public void UpdateClientImmunePieces(byte[] messagePackData)
        {
            var receivedData = MessagePackSerializer.Deserialize<GridGameIndices>(messagePackData);
            if (receivedData.SenderId != _wsHandler.ClientId)
            {
                //OnImmuneStatusReceived?.Invoke(receivedData.Index);
                _gameRef.HandleImmuneStatusReceived(receivedData.Index);
            }
        }

        private void MatchFound(string roomId)
        {
            Debug.Log("Found match and we're in the room called: " + roomId);
            _gameGUI.ShowMatchFoundScreen("Player Found! Ready?");
        }

        // private void ReceiveReadyState(bool isMyTurn)
        // {
        //     OnReadyStateReceived?.Invoke(isMyTurn);
        // }
        
        private void ReceiveReadyState(bool isMyTurn)
        {
            _gameRef.HandleReadyStateReceived(isMyTurn);
        }

        #endregion
    }
}
