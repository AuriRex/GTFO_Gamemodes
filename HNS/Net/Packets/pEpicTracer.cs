using System.Runtime.InteropServices;
using Gamemodes.Net.Packets.Data;

namespace HNS.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pEpicTracer
{
    public pVector3 Origin;
    public pVector3 Destination;
}