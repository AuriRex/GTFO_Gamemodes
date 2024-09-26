using System.Runtime.InteropServices;

namespace HNS.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct pHNSGameStart
{
    [MarshalAs(UnmanagedType.U1)]
    public byte SeekerCount;
    [MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
    public ulong[] Seekers;
    [MarshalAs(UnmanagedType.U1)]
    public byte SetupTimeSeconds;
}
