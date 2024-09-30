using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pWelcome
{
    [MarshalAs(UnmanagedType.I1)]
    public byte Hi;
}
