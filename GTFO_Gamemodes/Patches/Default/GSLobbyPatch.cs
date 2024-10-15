using Gamemodes.Mode;
using Gamemodes.Net;
using HarmonyLib;
using SNetwork;
using System.Linq;

namespace Gamemodes.Patches.Default;

[HarmonyPatch(typeof(GS_Lobby), nameof(GS_Lobby.TryStartLevelTrigger))]
internal class GSLobbyPatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = false;

        foreach (var player in SNet.LobbyPlayers)
        {
            NetworkingManager.GetPlayerInfo(player, out _);
        }

        NetworkingManager.CleanupPlayers();

        if (!NetworkingManager.AllPlayersVersionMatches)
        {
            Plugin.SendChatMessage("Version mismatch on some players:");

            foreach(var player in NetworkingManager.SyncedPlayers.Where(p => !p.VersionMatches))
            {
                Plugin.SendChatMessage($"{player.NickName}");
            }
            return false;
        }

        if (!NetworkingManager.AllPlayersHaveModeInstalled)
        {
            Plugin.SendChatMessage("Gamemode missing on some players:");

            foreach(var player in NetworkingManager.SyncedPlayers.Where(p => !p.HasModeInstalled(GamemodeManager.CurrentMode?.ID)))
            {
                Plugin.SendChatMessage($"{player.NickName}");
            }
            return false;
        }

        return true;
    }
}
