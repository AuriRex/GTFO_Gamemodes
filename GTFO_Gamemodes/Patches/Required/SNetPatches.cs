using HarmonyLib;
using SNetwork;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required
{
    [HarmonyWrapSafe]
    [HarmonyPatch(typeof(SNet_LobbyManager), nameof(SNet_LobbyManager.OnJoinedLobby))]
    internal static class SNet_LobbyManager_OnJoinedLobby_Patch
    {
        public static readonly string PatchGroup = PatchGroups.REQUIRED;

        public static void Postfix(SNet_Lobby lobby)
        {
            Plugin.L.LogDebug($"Invoking {nameof(GameEvents.OnJoinedLobby)}");
            GameEvents.InvokeOnJoinedLobby(lobby);
        }
    }

    [HarmonyPatch(typeof(SNet_SyncManager), nameof(SNet_SyncManager.OnFoundMaster))]
    internal static class SNet_SyncManager_OnFoundMaster_Patch
    {
        public static readonly string PatchGroup = PatchGroups.REQUIRED;

        public static void Postfix()
        {
            Plugin.L.LogDebug($"Invoking {nameof(GameEvents.OnFoundMaster)}");
            GameEvents.InvokeOnFoundMaster();
        }
    }
}
