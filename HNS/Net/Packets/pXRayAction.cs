using System.Runtime.InteropServices;
using Gamemodes.Net.Packets.Data;

namespace HNS.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pXRayAction
{
    public pVector3 Position;
    public pVector3 Direction;
}