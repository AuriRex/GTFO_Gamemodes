﻿using HarmonyLib;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(PlayerVoiceManager), nameof(PlayerVoiceManager.DoSayLine))]
internal class NoVoiceLines
{
    public static readonly string PatchGroup = PatchGroups.NO_VOICE;

    // TODO: Run for fall damage etc later once LobbyExpander properly works with player voices >= 4
    public static bool Prefix()
    {
        return false;
    }
}
