using System;

namespace Core.WebSocket
{
    public enum PacketType : byte
    {
        Chat = 0,
        Position = 1,
    }
    
    [Serializable]
    public class BaseWebSocketPackage
    {
        public byte SenderId;
        public PacketType Type;
        public ushort Sequence;
    }    
    
    [Serializable]
    public class ChatData : BaseWebSocketPackage
    {
        public string Text;
    }

    [Serializable]
    public class PositionData : BaseWebSocketPackage
    {
        public string ObjectId;
        public float X;
        public float Y;
        public float Z;
    }
}