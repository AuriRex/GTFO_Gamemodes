using CellMenu;
using Gamemodes.Components;
using Gamemodes.Extensions;
using Gamemodes.Mode;
using Gamemodes.Net;
using HarmonyLib;
using SNetwork;
using UnityEngine;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

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

            var player = token.player;

            NetworkingManager.GetPlayerInfo(player.Lookup, out var other);

            bool visible = GamemodeManager.CurrentMode.TeamVisibility.LocalPlayerCanSee(other.Team);

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
