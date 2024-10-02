using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pChatLogMessage
{
    public const int MAX_LENGTH = 256;

    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = MAX_LENGTH)]
    public string Content;
}
