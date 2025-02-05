using System.Runtime.InteropServices;
using Gamemodes.Net.Packets.Data;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pRayCast
{
    [MarshalAs(UnmanagedType.U1)]
    public byte Type;
    
    public pVector3 Origin;
    public pVector3 Direction;
}