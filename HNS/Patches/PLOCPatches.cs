using Gamemodes.Net;
using HarmonyLib;
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

        if ((_info.Team == (int)GMTeam.PreGameAndOrSpectator || !NetSessionManager.HasSession) && _info.IsLocal)
        {
            HideAndSeekMode.GameManager.ReviveLocalPlayer();
            return true;
        }

        if (!NetSessionManager.HasSession)
            return true;

        if (_info.Team == (int)GMTeam.Seekers)
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

        if (_info.Team == (int)GMTeam.Hiders)
        {
            NetworkingManager.AssignTeam(__instance.m_owner.Owner, (int)GMTeam.Seekers);
        }
    }
}