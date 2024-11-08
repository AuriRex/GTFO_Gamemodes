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

    public static float SlidePushForceMultiplier { get; set; } = 1f;
    public static float PushForceMultiplier { get; set; } = 1f;

    public static int PushForceReceivedFrame { get; private set; }
    
    public static Vector3 PushForceReceived { get; private set; }
    
    public static bool Prefix(PlayerLocomotion __instance, Vector3 force)
    {
        PushForceReceivedFrame = Time.frameCount;
        PushForceReceived = force;
        __instance.m_externalPushForce += force * PushForceMultiplier;
        __instance.m_hasExternalPushForce = true;
        return false;
    }
}

[HarmonyPatch(typeof(PLOC_Crouch), nameof(PLOC_Crouch.Enter))]
internal static class CrouchEnterPatch
{
    public static void Postfix(PLOC_Crouch __instance)
    {
        if (__instance.m_owner.Locomotion.m_lastStateEnum != PlayerLocomotion.PLOC_State.Run)
            return;

        if (PushForcePatch.PushForceReceivedFrame != Time.frameCount)
            return;
        
        // Same frame crouch entered from running state & push force received
        // Most likely a slide push
        __instance.m_owner.Locomotion.m_externalPushForce = PushForcePatch.PushForceReceived * PushForcePatch.SlidePushForceMultiplier;
    }
}