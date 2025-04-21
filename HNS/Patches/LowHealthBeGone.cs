using AK;
using Gamemodes.Net;
using HarmonyLib;
using HNS.Core;
using SNetwork;
using System.Collections.Generic;
using System.Reflection;

namespace HNS.Patches;

[HarmonyPatch]
internal class Dam_PlayerDamageLocal_NearDeath_Patches
{
    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        return new MethodBase[]
        {
            typeof(Dam_PlayerDamageLocal).GetMethod(nameof(Dam_PlayerDamageLocal.UpdateNearDeathAudioParam), AccessTools.all),
            typeof(Dam_PlayerDamageLocal).GetMethod(nameof(Dam_PlayerDamageLocal.SetNearDeathAudioEnabled), AccessTools.all),
        };
    }

    public static bool Prefix(Dam_PlayerDamageLocal __instance)
    {
        NetworkingManager.GetPlayerInfo(__instance.Owner.Owner, out var info);

        
        if (!TeamHelper.IsSeeker(info.Team))
            return true;

        __instance.Owner.Sound.SetRTPCValue(GAME_PARAMETERS.PLAYERHURT, 0f);
        __instance.Owner.Breathing.SetBreathingHealthLow(false);
        __instance.m_breathingHealthLow = false;
        __instance.m_nearDeathAudioPlaying = false;

        return false;
    }
}

[HarmonyPatch(typeof(PUI_LocalPlayerStatus), nameof(PUI_LocalPlayerStatus.StartHealthWarning))]
internal static class PUI_LocalPlayerStatus_StartHealthWarning_Patch
{
    public static bool Prefix(PUI_LocalPlayerStatus __instance)
    {
        NetworkingManager.GetPlayerInfo(SNet.LocalPlayer, out var info);

        if (!TeamHelper.IsSeeker(info.Team))
            return true;

        if (__instance.m_warningRoutine != null)
        {
            CoroutineManager.StopCoroutine(__instance.m_warningRoutine);
            __instance.m_warningRoutine = null;
        }

        return false;
    }
}

[HarmonyPatch(typeof(PlayerBreathing), nameof(PlayerBreathing.SetBreathingHealthLow))]
internal class PlayerBreathing_SetBreathingHealthLow_Patch
{
    public static bool Prefix(PlayerBreathing __instance)
    {
        var agent = __instance.m_owner;

        NetworkingManager.GetPlayerInfo(agent.Owner, out var info);

        if (!TeamHelper.IsSeeker(info.Team))
            return true;

        return false;
    }
}

[HarmonyPatch(typeof(PlayerBreathing), nameof(PlayerBreathing.Setup))]
internal static class PlayerBreathing_Setup_Patch
{
    public static void Postfix(PlayerBreathing __instance)
    {
        __instance.enabled = false;
    }
}
