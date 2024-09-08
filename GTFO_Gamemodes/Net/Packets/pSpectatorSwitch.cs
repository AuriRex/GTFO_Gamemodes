using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct pSpectatorSwitch
    {
        [MarshalAs(UnmanagedType.Bool)]
        public bool WantsToSpectate;
    }
}
