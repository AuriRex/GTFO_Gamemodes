using System.Runtime.InteropServices;

namespace HNS.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct pHNSGameStop
{
    [MarshalAs(UnmanagedType.I8)]
    public long Time;
    [MarshalAs(UnmanagedType.Bool)]
    public bool Aborted;
}
