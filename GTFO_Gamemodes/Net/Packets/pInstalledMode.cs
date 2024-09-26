using Gamemodes.Mode;
using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
internal struct pInstalledMode
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ModeInfo.ID_MAX_LENGTH)]
    public string GamemodeID;
}
