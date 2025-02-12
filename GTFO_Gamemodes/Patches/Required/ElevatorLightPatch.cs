using Gamemodes.Core;
using HarmonyLib;
using UnityEngine;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(ElevatorCage), nameof(ElevatorCage.StartPreReleaseSequence))]
public static class ElevatorLightPatch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;

    public static readonly Color DEFAULT_COLOR = new Color(0.2f, 0f, 0f, 1f);
    
    public static void Prefix(ElevatorCage __instance)
    {
        var color = GamemodeManager.CurrentMode?.GetElevatorColor() ?? DEFAULT_COLOR;

        __instance.BrakeLightColor = color;
    }
}