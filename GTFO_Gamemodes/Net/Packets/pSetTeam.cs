using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pSetTeam
{
    [MarshalAs(UnmanagedType.U8)]
    public ulong PlayerID;
    [MarshalAs(UnmanagedType.I4)]
    public int Team;
}
