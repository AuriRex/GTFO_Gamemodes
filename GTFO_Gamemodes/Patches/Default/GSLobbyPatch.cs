using Gamemodes.Net;
using HarmonyLib;

namespace Gamemodes.Patches.Default
{
    [HarmonyPatch(typeof(GS_Lobby), nameof(GS_Lobby.TryStartLevelTrigger))]
    internal class GSLobbyPatch
    {
        public static bool Prefix(ref bool __result)
        {
            __result = false;

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
}
