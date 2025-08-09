using HarmonyLib;
using SNetwork;

namespace Gamemodes.Patches.Default;

[HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.KickPlayer))]
public class GenerationChecksumPatch
{
    public static bool Prefix(SNet_Player player, SNet_PlayerEventReason reason)
    {
        if (reason == SNet_PlayerEventReason.Kick_GenerationChecksum)
        {
            Plugin.L.LogDebug($"Skipped kicking player \"{player.NickName}\" for generation checksum miss-match.");
            return false;
        }

        return true;
    }
}