using System;
using HarmonyLib;
using SNetwork;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(SNet_Replication), nameof(SNet_Replication.AllocateKey), new Type[] { typeof(SNet_ReplicatorType), typeof(ushort) })]
internal static class ReplicationPatch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;

    public static bool OverrideSelfManaged => OverrideCount > 0;
    public static uint OverrideCount { get; internal set; } = 0;
    
    public static bool Prefix(SNet_ReplicatorType type, ushort key, ref ushort __result)
    {
        if (!OverrideSelfManaged)
            return true;

        if (type != SNet_ReplicatorType.SelfManaged)
            return true;
        
        __result = (ushort)SNet_Replication.s_highestSlotUsed_SelfManaged;
        SNet_Replication.s_highestSlotUsed_SelfManaged++;
        
        return false;
    }
}