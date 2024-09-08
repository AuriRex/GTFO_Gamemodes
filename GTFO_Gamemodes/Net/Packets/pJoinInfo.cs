using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets
{
    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    public struct pJoinInfo
    {
        [MarshalAs(UnmanagedType.I4)]
        public int Major;
        [MarshalAs(UnmanagedType.I4)]
        public int Minor;
        [MarshalAs(UnmanagedType.I4)]
        public int Patch;
    }
}
