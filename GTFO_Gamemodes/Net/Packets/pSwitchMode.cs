﻿using Gamemodes.Core;
using System.Runtime.InteropServices;

namespace Gamemodes.Net.Packets;

[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
public struct pSwitchMode
{
    [MarshalAs(UnmanagedType.ByValTStr, SizeConst = ModeInfo.ID_MAX_LENGTH)]
    public string GamemodeID;
}
