using System.Runtime.InteropServices;
using Gamemodes.Net.Packets.Data;

namespace ExtraModes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pBoxAction
{
    [MarshalAs(UnmanagedType.U2)]
    public ushort ID;
    
    [MarshalAs(UnmanagedType.U1)]
    public byte Action;

    public pVector3 Position;
    public pQuaternion Rotation;
    public pVector3 Scale;
}