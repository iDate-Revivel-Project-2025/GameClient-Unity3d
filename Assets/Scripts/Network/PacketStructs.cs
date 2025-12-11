using System.Runtime.InteropServices;

namespace iDate.Network.Structs
{
    // 0x457520 RE Findings:
    // Packet 1000 (Login Response?)
    // Offset 0: Header
    // Offset 4: Result (UShort)
    // Offset 8: UserID? (Int)
    // Offset 13: Permission/Status (Byte)
    
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct LoginResponseHeader
    {
        public ushort ResultCode; // 0=Success?
        public uint UserID;
        public byte Status;
        public uint ServerTime;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RoomEntry
    {
        public ushort RoomID;
        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 20)]
        public string RoomName; // Fixed size usually
        public byte MaxPlayers;
        public byte CurrentPlayers;
        public byte IsPasswordProtected; // 0 or 1
        public byte State; // 0=Wait, 1=Playing
    }

    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct RoomListPacket
    {
        public ushort Count;
        // Followed by Count * RoomEntry
    }
}
