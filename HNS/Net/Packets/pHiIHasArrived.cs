using System.Runtime.InteropServices;

namespace HNS.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pHiIHasArrived
{
    [MarshalAs(UnmanagedType.U1)]
    public byte hi;
}