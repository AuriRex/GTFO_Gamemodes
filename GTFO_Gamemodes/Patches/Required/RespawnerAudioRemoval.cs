using HarmonyLib;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(EnemyRespawnerVisual), nameof(EnemyRespawnerVisual.Start))]
internal static class EnemyRespawnerVisual__Start__Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static bool Prefix()
    {
        return false;
    }
}