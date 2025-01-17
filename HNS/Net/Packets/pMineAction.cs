using System.Runtime.InteropServices;

namespace HNS.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pMineAction
{
    [MarshalAs(UnmanagedType.U1)]
    public byte state;
    [MarshalAs(UnmanagedType.U2)]
    public ushort mineReplicatorKey;
}