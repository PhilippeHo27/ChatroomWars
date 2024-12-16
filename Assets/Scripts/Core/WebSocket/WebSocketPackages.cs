using System;
using System.Collections.Generic;
using UnityEngine;

namespace Core.WebSocket
{
    [Serializable]
    public class WebSocketPackage
    {
        public string type;
    }

    [Serializable]
    public class PositionData : WebSocketPackage
    {
        public string objectId;
        public float x;
        public float y;
        public float z;
    }

    [Serializable]
    public class ChatData : WebSocketPackage
    {
        public string text;
    }
    
    [Serializable]
    public class TypingProgressData : WebSocketPackage
    {
        public string playerId;
        public string currentText;
        public float progress;
    }
    
    // Future packages that will likely be needed :

    [Serializable]
    public class PlayerStateData : WebSocketPackage
    {
        public string playerId;
        public string state;  // "waiting", "ready", "typing", "finished"
        public float wpm;     // words per minute
        public int accuracy;  // typing accuracy percentage
    }

    [Serializable]
    public class RoomData : WebSocketPackage
    {
        public string roomId;
        public string[] playerIds;
        public string roomState;  // "waiting", "full", "in_game", "finished"
    }

    [Serializable]
    public class GameStateData : WebSocketPackage
    {
        public string roomId;
        public string textToType;  // the challenge text
        public float timeRemaining;
        public bool isGameOver;
    }

    [Serializable]
    public class MatchResultData : WebSocketPackage
    {
        public string winnerId;
        public Dictionary<string, PlayerStats> PlayerStats;
    }

    [Serializable]
    public class PlayerStats
    {
        public float finalWpm;
        public int accuracy;
        public float timeElapsed;
    }
}