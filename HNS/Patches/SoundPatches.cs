using Gamemodes.Net;
using HarmonyLib;
using HNS.Core;

namespace HNS.Patches;

[HarmonyPatch(typeof(PLOC_Run), nameof(PLOC_Run.CommonUpdate))]
internal static class PLOC_Run__CommonUpdate__Patch
{
    public static bool Prefix(PLOC_Run __instance)
    {
        NetworkingManager.GetPlayerInfo(__instance.m_owner.Owner, out var info);

        Plugin.L.LogDebug($"{nameof(PLOC_Run__CommonUpdate__Patch)}: {info.NickName}: {(GMTeam) info.Team}");
        
        if (info.Team == (int)GMTeam.Camera)
            return false;
        
        return true;
    }
}

// Inlined into both Exit() and SyncExit()
[HarmonyPatch(typeof(PLOC_Land), nameof(PLOC_Land.SyncExit))]
internal static class PLOC_Land__CommonExit__Patch
{
    public static bool Prefix(PLOC_Land __instance)
    {
        NetworkingManager.GetPlayerInfo(__instance.m_owner.Owner, out var info);

        Plugin.L.LogDebug($"{nameof(PLOC_Land__CommonExit__Patch)}: {info.NickName}: {(GMTeam) info.Team}");
        
        if (info.Team == (int)GMTeam.Camera)
            return false;
        
        return true;
    }
}

[HarmonyPatch(typeof(PLOC_Jump), nameof(PLOC_Jump.CommonEnter))]
internal static class PLOC_Jump__CommonEnter__Patch
{
    public static bool Prefix(PLOC_Jump __instance)
    {
        NetworkingManager.GetPlayerInfo(__instance.m_owner.Owner, out var info);

        Plugin.L.LogDebug($"{nameof(PLOC_Jump__CommonEnter__Patch)}: {info.NickName}: {(GMTeam) info.Team}");
        
        if (info.Team == (int)GMTeam.Camera)
            return false;
        
        return true;
    }
}