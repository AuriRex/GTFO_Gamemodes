using HarmonyLib;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckExpeditionFailed))]
internal static class WardenObjectiveManager_CheckExpeditionFailed_Patch
{
    public static readonly string PatchGroup = PatchGroups.NO_FAIL;

    public static bool Prefix(ref bool __result)
    {
        __result = false;
        return false;
    }
}
