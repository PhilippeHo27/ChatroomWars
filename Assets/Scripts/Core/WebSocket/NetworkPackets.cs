using MessagePack;
using UnityEngine;

namespace Core.WebSocket
{
    public enum PacketType : byte
    {
        Chat = 0,
        Position = 1,
        IdAssign = 2,
        TimeSync = 3,

        RoomCreate = 4,
        RoomJoin = 5,
        RoomLeave = 6,
        RoomDestroy = 7,
    
        ServerResponse = 8,
        UserInfo = 9,
    
        VinceGamePacket = 10,
        VinceGameImmune = 11,
        VinceGameConfirmStart = 12,
        MatchmakingRequest = 13,
        MatchFound = 14,
    
        // New packet types for the ready system:
        GameStartInfo  = 15,
        OpponentDisconnected = 17,
        ExtraTurnPacket = 18
    }
    
    [MessagePackObject]
    public class BaseNetworkPacket
    {
        [Key(0)]
        public byte SenderId { get; set; }  
    
        [Key(1)]
        public PacketType Type { get; set; }
    }    
    [MessagePackObject]
    public class BooleanPacket : BaseNetworkPacket
    {
        [Key(2)]
        public bool Response { get; set; }
    }
    
    [MessagePackObject]
    public class MatchmakingRequest : BaseNetworkPacket
    {
        [Key(2)]
        public bool IsSearching { get; set; }
    }

    [MessagePackObject]
    public class StringPacket : BaseNetworkPacket
    {
        [Key(2)]
        public string Text { get; set; }
    }

    [MessagePackObject]
    public class ObjectVector3Packet : BaseNetworkPacket
    {
        [Key(2)]
        public string ObjectId { get; set; }
    
        [Key(3)]
        public Vector3 Position { get; set; }
    }
    
    [MessagePackObject]
    public class RoomAction : BaseNetworkPacket
    {
        [Key(2)]
        public string RoomId { get; set; }
    }
    
    [MessagePackObject]
    public class UserListUpdate : BaseNetworkPacket
    {
        [Key(2)]
        public UserEntry[] Users { get; set; }
    }
    
    [MessagePackObject]
    public struct UserEntry
    {
        [Key(0)]
        public byte UserId { get; set; }

        [Key(1)]
        public string UserName { get; set; }
    }

    [MessagePackObject]
    public class VinceGameData : BaseNetworkPacket
    {
        [Key(2)] 
        public byte Index; // 0 to 9 which case was selected?
        
        [Key(3)]
        public string SquareColor { get; set; } // string for now but it can easily be bytes later
    }
    
    [MessagePackObject]
    public class ExtraTurnPacket  : BaseNetworkPacket
    {
        [Key(2)]
        public byte[] Indices { get; set; } // Array of positions that were played
    
        [Key(3)]
        public string[] Colors { get; set; } // Corresponding colors for each position
    }
    
    [MessagePackObject]
    public class GridGameIndices : BaseNetworkPacket
    {
        [Key(2)] 
        public byte[] Index;
    }
}
