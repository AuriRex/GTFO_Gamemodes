using HarmonyLib;
using Player;
using UnityEngine;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPriority(Priority.Last)]
[HarmonyPatch(typeof(PlayerLocomotion), nameof(PlayerLocomotion.AddExternalPushForce))]
internal class PushForcePatch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;

    public static float PushForceMultiplier { get; set; } = 1f;

    public static bool Prefix(PlayerLocomotion __instance, Vector3 force)
    {
        __instance.m_externalPushForce += force * PushForceMultiplier;
        __instance.m_hasExternalPushForce = true;
        return false;
    }
}
