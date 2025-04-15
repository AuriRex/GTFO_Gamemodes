using System.Linq;
using CellMenu;
using Gamemodes.Components;
using Gamemodes.Extensions;
using Gamemodes.Core;
using Gamemodes.Net;
using HarmonyLib;
using Player;
using SNetwork;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(PlayerSyncModelData), nameof(PlayerSyncModelData.RefreshGhostRenderersVisibility))]
internal static class PlayerSyncModelData_RefreshGhostRenderersVisibility_Patch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;

    public static void Prefix(PlayerSyncModelData __instance)
    {
        if (__instance == null || __instance.Owner == null || __instance.Owner.Owner == null)
            return;
        
        var visible = TeamVisibility.LocalPlayerCanSee(__instance.Owner.Owner);

        __instance.m_ghostEnabled = visible && __instance.m_ghostEnabled;
    }
}

//private void UpdateGUIElementsVisibility(eFocusState currentState)
[HarmonyPatch(typeof(PlayerGuiLayer), nameof(PlayerGuiLayer.UpdateGUIElementsVisibility))]
internal static class PlayerGuiLayer_UpdateGUIElementsVisibility_Patch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;

    public static void Postfix(eFocusState currentState)
    {
        switch (currentState)
        {
            default:
                break;
            case eFocusState.FPS:
            case eFocusState.FPS_TypingInChat:
            case eFocusState.FPS_CommunicationDialog:
                var localPlayer = PlayerManager.GetLocalPlayerAgent().TryCast<LocalPlayerAgent>();

                if (localPlayer == null)
                    break;
                
                localPlayer.SetTeammateInfoVisible(true);
                break;
        }
    }
}

[HarmonyPatch(typeof(PUI_Compass), nameof(PUI_Compass.AfterCameraUpdate))]
internal static class PUI_Compass_AfterCameraUpdate_Patch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;

    public static void Postfix(PUI_Compass __instance)
    {
        for (int i = 0; i < __instance.m_playerNameMarkersVisible.Length; i++)
        {
            if (!__instance.m_playerNameMarkersVisible[i] && !__instance.m_playerPingMarkersActive[i])
            {
                continue;
            }

            PlayerAgent player = PlayerManager.Current.GetPlayerAgentInSlot(i);

            if (player == null || player.Owner == null)
                continue;

            if (player.IsLocallyOwned && !TeamVisibility.LocalPlayerHideIcons())
            {
                continue;
            }
            
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

[HarmonyPatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.SetTeammateInfoVisible))]
internal static class LocalPlayerAgent_SetTeammateInfoVisible_Patch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;

    public static bool Prefix(LocalPlayerAgent __instance, bool value)
    {
        foreach(var playerAgent in PlayerManager.PlayerAgentsInLevel)
        {
            if (playerAgent == null)
                continue;

            if (playerAgent.IsLocallyOwned)
                continue;

            value = TeamVisibility.LocalPlayerCanSee(playerAgent.Owner);

            playerAgent.NavMarker.SetMarkerVisible(value);
        }

        __instance.m_teammatesVisible = true;
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

            if (guiItem.IsLocal && !TeamVisibility.LocalPlayerHideIcons())
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

//public void OnStateChange(pNavMarkerState oldState, pNavMarkerState newState, bool isDropinState)
[HarmonyPatch(typeof(SyncedNavMarkerWrapper), nameof(SyncedNavMarkerWrapper.OnStateChange))]
internal static class SyncedNavMarkerWrapper__OnStateChange__Patch
{
    public static readonly string PatchGroup = PatchGroups.USE_TEAM_VISIBILITY;
    
    public static bool Prefix(SyncedNavMarkerWrapper __instance, pNavMarkerState newState)
    {
        var playerInfo = NetworkingManager.AllValidPlayers.FirstOrDefault(pw =>
            pw.NetPlayer.PlayerSlotIndex() == __instance.m_playerIndex);

        if (playerInfo == null)
            return true;

        if (newState.status == eNavMarkerStatus.Hidden)
            return true;


        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return true;

        var isLocal = localPlayer.PlayerSlotIndex == __instance.m_playerIndex;
        
        if (isLocal && !TeamVisibility.LocalPlayerHideIcons())
        {
            // Local player should be visible :)
            return true;
        }
        
        bool visible = TeamVisibility.LocalPlayerCanSee(playerInfo);

        //Plugin.L.LogDebug($"SyncedNavMarkerWrapper__OnStateChange__Patch: {playerInfo.NickName}: {visible}");
        
        return visible;
    }
}
