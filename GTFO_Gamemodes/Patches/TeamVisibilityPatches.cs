using CellMenu;
using Gamemodes.Components;
using Gamemodes.Extensions;
using Gamemodes.Mode;
using HarmonyLib;
using Player;
using SNetwork;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

// teaminfo marker:
// LocalPlayerAgent
// private void SetTeammateInfoVisible(bool value)

// PlayerAgent
// public virtual void Setup(int characterID)

//AfterCameraUpdate()
[HarmonyPatch(typeof(PUI_Compass), nameof(PUI_Compass.AfterCameraUpdate))]
internal static class PUI_Compass_AfterCameraUpdate_Patch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;

    public static void Postfix(PUI_Compass __instance)
    {
        for (int i = 0; i < __instance.m_playerNameMarkersVisible.Length; i++)
        {
            if (__instance.m_playerNameMarkersVisible[i] || __instance.m_playerPingMarkersActive[i])
            {
                PlayerAgent player = PlayerManager.Current.GetPlayerAgentInSlot(i);

                var visible = TeamVisibility.LocalPlayerCanSee(player.Owner);

                if (visible)
                {
                    continue;
                }

                __instance.m_playerNameMarkers[i].SetVisible(false);
                __instance.m_playerPingMarkers[i].SetVisible(false);
                __instance.m_playerNameMarkersVisible[i] = false;
                __instance.m_playerPingMarkersActive[i] = false;
            }
        }
    }
}

[HarmonyPatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.SetTeammateInfoVisible))]
internal static class LocalPlayerAgent_SetTeammateInfoVisible_Patch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;

    public static bool Prefix(LocalPlayerAgent __instance, bool value)
    {
        foreach(var player in SNet.Slots.SlottedPlayers)
        {
            if (player == null || !player.HasPlayerAgent || player.IsLocal)
            {
                continue;
            }

            PlayerAgent playerAgent = player.PlayerAgent.Cast<PlayerAgent>();
            
            if (playerAgent == null)
            {
                continue;
            }

            var visible = TeamVisibility.LocalPlayerCanSee(player);

            playerAgent.NavMarker.SetMarkerVisible(value && visible);
        }

        __instance.m_teammatesVisible = value;
        return false;
    }
}

[HarmonyPatch(typeof(CM_PageMap), nameof(CM_PageMap.UpdatePlayerData))]
internal class Map_UpdatePlayerData_VisibilityPatch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;

    public static void Postfix(CM_PageMap __instance)
    {
        foreach(var guiItem in __instance.m_syncedPlayers)
        {
            if (guiItem == null)
                continue;

            if (guiItem.IsLocal)
            {
                // Local player should be visible :)
                continue;
            }

            if (!guiItem.gameObject.TryGetComponentButDontCrash<PlayerToken>(out var token))
            {
                continue;
            }

            bool visible = TeamVisibility.LocalPlayerCanSee(token.player);

            guiItem.SetVisible(visible);
        }
    }
}

[HarmonyPatch(typeof(CM_MapPlayerGUIItem), nameof(CM_MapPlayerGUIItem.SetPlayer))]
internal static class MapSyncPlayerGuiItem_PlayerTokenSetter_Patch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;

    public static void Postfix(CM_MapPlayerGUIItem __instance, SNet_Player player)
    {
        var id = __instance.gameObject.GetOrAddComponent<PlayerToken>();

        id.player = player;
    }
}
