using GameData;
using HarmonyLib;
using LevelGeneration;
using System;
using System.Linq;
using static Gamemodes.Patches.Builder_Build_Patch;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(Builder), nameof(Builder.Build))]
internal class Builder_Build_Patch
{
    public static readonly string PatchGroup = PatchGroups.FORCE_ARENA_DIM;

    internal static bool hasAddedArenaDimension = false;
    internal static ExpeditionInTierData activeExpedition;
    internal static DimensionInExpeditionData[] originalDDs;

    public static void Prefix()
    {
        var exp = RundownManager.Current.m_activeExpedition;

        if (!exp.DimensionDatas.ToArray().Any(dd => dd.DimensionIndex == eDimensionIndex.ARENA_DIMENSION && dd.DimensionData == 14))
        {
            Plugin.L.LogDebug("Forcefully adding a snatcher dim.");

            originalDDs = exp.DimensionDatas.ToArray();

            // Snatcher dim
            exp.DimensionDatas.Add(new()
            {
                DimensionData = 14,
                DimensionIndex = eDimensionIndex.ARENA_DIMENSION,
                Enabled = true,
            });

            hasAddedArenaDimension = true;
            activeExpedition = exp;
        }
    }
}

[HarmonyPatch(typeof(Builder), nameof(Builder.OnLevelCleanup))]
internal class Builder_OnLevelCleanup_Patch
{
    public static readonly string PatchGroup = PatchGroups.FORCE_ARENA_DIM;

    public static void Prefix()
    {
        if (hasAddedArenaDimension)
        {
            Plugin.L.LogDebug("Reverting forced snatcher dim.");
            var list = new Il2CppSystem.Collections.Generic.List<DimensionInExpeditionData>();

            foreach (var dimData in originalDDs)
            {
                list.Add(dimData);
            }

            activeExpedition.DimensionDatas = list;

            activeExpedition = null;
            hasAddedArenaDimension = false;
            originalDDs = Array.Empty<DimensionInExpeditionData>();
        }
    }
}
