using Gamemodes.Net;
using HarmonyLib;
using SNetwork;

namespace Gamemodes.Patches.Default;

[HarmonyPatch(typeof(GS_Lobby), nameof(GS_Lobby.TryStartLevelTrigger))]
internal class GSLobbyPatch
{
    public static bool Prefix(ref bool __result)
    {
        __result = false;

        foreach (var player in SNet.LobbyPlayers)
        {
            NetworkingManager.GetPlayerInfo(player.Lookup, out _);
        }

        if (!NetworkingManager.AllPlayersVersionMatches)
        {
            Plugin.SendChatMessage("Version mismatch on some players");
            return false;
        }

        if (!NetworkingManager.AllPlayersHaveModeInstalled)
        {
            Plugin.SendChatMessage("Gamemode missing on some players");
            return false;
        }

        return true;
    }
}
