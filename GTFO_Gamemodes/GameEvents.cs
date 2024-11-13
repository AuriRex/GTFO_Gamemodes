using SNetwork;
using System;

namespace Gamemodes;

public static class GameEvents
{
    public static event Action OnGameDataInit;
    public static event Action OnGameSessionStart;
    public static event Action<ExpeditionEndState, (int Muted, int Bold, int Aggressive)> OnGameSessionEnd;
    public static event Action<SNet_Lobby> OnJoinedLobby;
    public static event Action<eGameStateName> OnGameStateChanged;
    public static event Action OnFoundMaster;
    public static event Action PreItemPrefabsSetup;
    public static event Action OnItemPrefabsSetup;

    internal static void InvokeOnGameDataInit() => OnGameDataInit?.Invoke();
    internal static void InvokeOnGameSessionStart() => OnGameSessionStart?.Invoke();
    internal static void InvokeOnGameSessionEnd(ExpeditionEndState state, (int Muted, int Bold, int Aggressive) artifacts) => OnGameSessionEnd?.Invoke(state, artifacts);
    internal static void InvokeOnJoinedLobby(SNet_Lobby lobby) => OnJoinedLobby?.Invoke(lobby);
    internal static void InvokeOnGameStateChanged(eGameStateName nextState) => OnGameStateChanged?.Invoke(nextState);
    internal static void InvokeOnFoundMaster() => OnFoundMaster?.Invoke();
    internal static void InvokePreItemPrefabsSetup() => PreItemPrefabsSetup?.Invoke();
    internal static void InvokeOnItemPrefabsSetup() => OnItemPrefabsSetup?.Invoke();
}
