using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pForcedTeleport
{
    [MarshalAs(UnmanagedType.I1)]
    public byte DimensionIndex;

    [MarshalAs(UnmanagedType.R4)]
    public float PosX;
    [MarshalAs(UnmanagedType.R4)]
    public float PosY;
    [MarshalAs(UnmanagedType.R4)]
    public float PosZ;

    [MarshalAs(UnmanagedType.R4)]
    public float DirX;
    [MarshalAs(UnmanagedType.R4)]
    public float DirY;
    [MarshalAs(UnmanagedType.R4)]
    public float DirZ;

    [MarshalAs(UnmanagedType.I1)]
    public byte WarpOptions;
}
