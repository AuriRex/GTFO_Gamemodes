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
    public static bool HasOverrideID => OverrideID != 0;

    public static int HighestSlotUsed_SelfManaged
    {
        get => SNet_Replication.s_highestSlotUsed_SelfManaged;
        set => SNet_Replication.s_highestSlotUsed_SelfManaged = value;
    }
    
    public static ushort OverrideID { get; internal set; }
    public static uint OverrideCount { get; internal set; } = 0;
    
    public static bool Prefix(SNet_ReplicatorType type, ushort key, ref ushort __result)
    {
        if (!OverrideSelfManaged)
            return true;

        if (type != SNet_ReplicatorType.SelfManaged)
            return true;

        if (HasOverrideID)
        {
            __result = OverrideID;
            HighestSlotUsed_SelfManaged = OverrideID + 1;
            OverrideID = 0;
        }
        else
        {
            __result = (ushort)SNet_Replication.s_highestSlotUsed_SelfManaged;
            SNet_Replication.s_highestSlotUsed_SelfManaged++;
        }

        return false;
    }
}