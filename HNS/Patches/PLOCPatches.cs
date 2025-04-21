using Gamemodes.Net;
using HarmonyLib;
using HNS.Components;
using HNS.Core;
using HNS.Net;

namespace HNS.Patches;

[HarmonyPatch(typeof(PLOC_Downed), nameof(PLOC_Downed.Enter))]
internal static class PLOC_Downed_Patch
{
    private static PlayerWrapper _info;

    public static bool Prefix(PLOC_Downed __instance)
    {
        NetworkingManager.GetPlayerInfo(__instance.m_owner.Owner, out _info);

        var simpleTeam = HideAndSeekMode.SimplifyTeam((GMTeam) _info.Team);
        
        if ((simpleTeam == (int)GMTeam.PreGame || !NetSessionManager.HasSession) && _info.IsLocal)
        {
            NetworkingManager.PostChatLog($"{_info.PlayerColorTag}{_info.NickName}</color> <color=orange>got bopped!</color>");
            HideAndSeekMode.GameManager.ReviveLocalPlayer();
            return true;
        }

        if (!NetSessionManager.HasSession)
            return true;
        
        if (HideAndSeekMode.IsSeeker(_info.Team) || _info.Team == (int)GMTeam.Camera)
        {
            __instance.m_owner.Locomotion.ChangeState(__instance.m_owner.Locomotion.m_lastStateEnum);
            return false;
        }

        return true;
    }

    public static void Postfix(PLOC_Downed __instance)
    {
        if (!NetSessionManager.HasSession)
            return;
        
        if (!HideAndSeekMode.IsHider(_info.Team))
            return;

        SpectatorController.TryExit();
        
        var seekerTeam = HideAndSeekMode.GetSeekerTeamForHiders((GMTeam)_info.Team);
        NetworkingManager.AssignTeam(__instance.m_owner.Owner, (int)seekerTeam);
    }
}