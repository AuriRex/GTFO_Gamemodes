using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pGearChangeNotif
{
    [MarshalAs(UnmanagedType.U4)]
    public uint gearChecksumPrevious;
    
    [MarshalAs(UnmanagedType.U4)]
    public uint gearChecksum;

    [MarshalAs(UnmanagedType.Bool)]
    public bool isGun;
    
    [MarshalAs(UnmanagedType.Bool)]
    public bool isTool;
}