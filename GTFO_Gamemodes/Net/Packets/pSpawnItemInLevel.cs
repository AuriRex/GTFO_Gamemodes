using System.Runtime.InteropServices;
using Gamemodes.Net.Packets.Data;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pSpawnItemInLevel
{
    public pVector3 Position;

    public pCourseNode Node;
    
    [MarshalAs(UnmanagedType.U4)]
    public uint ItemID;
    
    [MarshalAs(UnmanagedType.R4)]
    public float AmmoMultiplier;
}