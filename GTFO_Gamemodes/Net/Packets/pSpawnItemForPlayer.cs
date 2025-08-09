using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pSpawnItemForPlayer
{
    [MarshalAs(UnmanagedType.U8)]
    public ulong PlayerID;
    
    [MarshalAs(UnmanagedType.U4)]
    public uint ItemID;
    
    [MarshalAs(UnmanagedType.R4)]
    public float AmmoMultiplier;

    [MarshalAs(UnmanagedType.Bool)]
    public bool DoWield;
    
    [MarshalAs(UnmanagedType.U2)]
    public ushort ReplicatorKey;
}