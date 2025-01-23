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

        // Room Packet types
        RoomCreate = 4,
        RoomJoin = 5,
        RoomLeave = 6,
        RoomDestroy = 7,
        
        // Server messages
        ServerResponse = 8
    }

    
    [MessagePackObject]
    public class BaseWebSocketPackage
    {
        [Key(0)]
        public byte SenderId { get; set; }
    
        [Key(1)]
        public PacketType Type { get; set; }
    }    

    [MessagePackObject]
    public class ChatData : BaseWebSocketPackage
    {
        [Key(2)]
        public string Text { get; set; }
    }
    
    [MessagePackObject]
    public class PositionDataVector : BaseWebSocketPackage
    {
        [Key(2)]
        public string ObjectId { get; set; }
    
        [Key(3)]
        public Vector3 Position { get; set; }
    }
    
    [MessagePackObject]
    public class RoomAction : BaseWebSocketPackage
    {
        [Key(2)]
        public string RoomId { get; set; }
    }
}
