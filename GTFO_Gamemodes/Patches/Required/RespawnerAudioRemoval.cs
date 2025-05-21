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

// Tiny blobs in R8B1 dim 1 for example, nightmare blobs or whatever
[HarmonyPatch(typeof(Infestation_BaseBlob), nameof(Infestation_BaseBlob.Start))]
internal static class Infestation_BaseBlob__Start__Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static bool Prefix()
    {
        return false;
    }
}