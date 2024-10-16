using CellMenu;
using Gamemodes.UI;
using HarmonyLib;
using System;
using UnityEngine;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(CM_PageLoadout), nameof(CM_PageLoadout.Setup))]
internal class LobbyPatch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;

    public static void Postfix(CM_PageLoadout __instance)
    {
        LobbyUI.Setup(__instance);
    }
}
