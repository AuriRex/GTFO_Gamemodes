using Gamemodes.Net;
using HarmonyLib;
using SNetwork;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

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

[HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.AddPlayerToSession))]
internal static class NetworkingPatches
{
    public static void Prefix(SNet_Player player)
    {
        NetworkingManager.OnPlayerAddedToSession(player);
    }
}

[HarmonyPatch(typeof(SNet_SessionHub), nameof(SNet_SessionHub.OnLeftLobby))]
internal static class SNet_SessionHub__OnLeftLobby__Patch
{
    public static void Prefix(SNet_Player player)
    {
        NetworkingManager.OnPlayerRemovedFromSession(player);
    }
}

[HarmonyPatch(typeof(SNet_GlobalManager), nameof(SNet_GlobalManager.OnPlayerEvent))]
internal static class SNet_GlobalManager__OnPlayerEvent__Patch
{
    public static void Postfix(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason)
    {
        if (SNet.IsMaster)
            return;
        
        switch (playerEvent)
        {
            case SNet_PlayerEvent.PlayerKickedFromSession:
            case SNet_PlayerEvent.PlayerLeftSessionHub:
                NetworkingManager.OnPlayerRemovedFromSession(player);
                break;
        }
    }
}
