using Gamemodes.Mode;
using GTFO.API;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Gamemodes.Net;

public partial class NetworkingManager
{
    private const string PREFIX = "CGamemodes_";

    private static readonly Dictionary<Type, string> _eventNameLookup = new();
    private static readonly Dictionary<Type, Delegate> _eventStorage = new();

    private static readonly Dictionary<ulong, PlayerWrapper> _syncedPlayers = new();

    public static IEnumerable<PlayerWrapper> AllValidPlayers => _syncedPlayers.Values.Where(p => p.ValidPlayer);

    public static IEnumerable<PlayerWrapper> Spectators => _syncedPlayers.Values.Where(p => p.IsSpectator);

    public static ulong LocalPlayerId => SNet.LocalPlayer?.Lookup ?? 0;

    public static bool AllPlayersVersionMatches => _syncedPlayers.Values.All(pi => pi.VersionMatches);

    public static bool AllPlayersHaveModeInstalled => _syncedPlayers.Values.All(pi => pi.HasModeInstalled(GamemodeManager.CurrentMode?.ID));

    public static bool InLevel => GameStateManager.CurrentStateName == eGameStateName.InLevel;

    /// <summary>
    /// PlayerWrapper => Master
    /// </summary>
    public static event Action<PlayerWrapper> OnJoinedLobbySyncEvent;

    internal static void Init()
    {
        GameEvents.OnGameDataInit += OnGameDataInit;
    }

    private static void OnGameDataInit()
    {
        Plugin.L.LogDebug("Registering base events ...");
        RegisterAllEvents();

        SNet_Events.OnPlayerEvent += (Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason>)OnPlayerEvent;
        SNet_Events.OnPlayerJoin += (Action)OnPlayerJoined;
        SNet_Events.OnPlayerLeave += (Action)OnPlayerCountChanged;
    }

    private static void OnPlayerJoined()
    {
        SNet_Player newPlayer = null;
        foreach (var player in SNet.LobbyPlayers)
        {
            var pw = _syncedPlayers.Values.FirstOrDefault(pw => pw.ID == player.Lookup);
            if (pw == null)
            {
                newPlayer = player;
                break;
            }
        }

        if (newPlayer != null)
        {
            GetPlayerInfo(newPlayer.Lookup, out var info);
            Plugin.L.LogDebug($"{newPlayer.NickName} has joined!");
            SendWelcome(newPlayer);
            SendSwitchModeTo(GamemodeManager.CurrentMode.ID, newPlayer);
        }

        OnPlayerCountChanged();
    }

    private static void OnPlayerCountChanged()
    {
        Plugin.L.LogDebug($"OnPlayerCountChanged");
        CleanupPlayers();
    }

    private static void OnPlayerEvent(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason)
    {
        Plugin.L.LogDebug($"OnPlayerEvent: {player.NickName}, event: {playerEvent}, reason: {reason}");
    }

    private static void CleanupPlayers()
    {
        HashSet<ulong> connectedPlayers = new();
        foreach (var player in SNet.LobbyPlayers)
        {
            connectedPlayers.Add(player.Lookup);
        }

        var disconnectedPlayers = _syncedPlayers.Values.Where(p => !connectedPlayers.Contains(p.ID));

        if (disconnectedPlayers.Count() == 0)
            return;

        Plugin.L.LogWarning($"Cleanup: Removing {disconnectedPlayers.Count()} players");
        foreach (var player in disconnectedPlayers)
        {
            _syncedPlayers.Remove(player.ID);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="playerId"></param>
    /// <param name="playerInfo"></param>
    /// <param name="forceReset"></param>
    /// <returns>True if newly created</returns>
    public static bool GetPlayerInfo(ulong playerId, out PlayerWrapper playerInfo, bool forceReset = false)
    {
        if (!forceReset && _syncedPlayers.TryGetValue(playerId, out playerInfo))
        {
            return false;
        }

        if (_syncedPlayers.ContainsKey(playerId))
        {
            _syncedPlayers.Remove(playerId);
        }

        playerInfo = new(playerId);
        Plugin.L.LogWarning($"Synced: {playerId}: {playerInfo}");
        _syncedPlayers.Add(playerId, playerInfo);
        return true;
    }

    public static bool GetPlayerInfo(SNet_Player player, out PlayerWrapper playerInfo, bool forceReset = false)
    {
        return GetPlayerInfo(player.Lookup, out playerInfo, forceReset);
    }

    internal static PlayerWrapper GetLocalPlayerInfo()
    {
        _syncedPlayers.TryGetValue(LocalPlayerId, out var playerInfo);

        return playerInfo;
    }

    public static bool TryGetSender(ulong senderId, out SNet_Player sender)
    {
        return SNet.TryGetPlayer(senderId, out sender);
    }

    public static bool IsLocalSender(ulong senderId)
    {
        return LocalPlayerId == senderId;
    }


    public static void SendEventAndInvokeLocally<T>(T data, SNet_Player targetPlayer = null) where T : struct
    {
        SendEvent(data, targetPlayer, invokeLocal: true);
    }

    public static void SendEvent<T>(T data, SNet_Player targetPlayer = null, bool invokeLocal = false) where T : struct
    {
        var eventName = GetEventName<T>();

        if (!NetworkAPI.IsEventRegistered(eventName))
            throw new ArgumentException($"The provided type \"{typeof(T).Name}\" has not been registered as a valid event. ({eventName})", nameof(data));

        if (targetPlayer == null)
        {
            NetworkAPI.InvokeEvent(eventName, data);
        }
        else
        {
            NetworkAPI.InvokeEvent(eventName, data, targetPlayer);
        }

        if (!invokeLocal)
            return;

        if (!TryGetEvent<T>(out var eventAction))
            return;

        eventAction.Invoke(LocalPlayerId, data);
    }

    public static void RegisterEvent<T>(Action<ulong, T> onReceive) where T : struct
    {
        var eventName = GetEventName<T>();

        if (NetworkAPI.IsEventRegistered(eventName))
            return;

        _eventStorage.Add(typeof(T), onReceive);

        NetworkAPI.RegisterEvent<T>(eventName, onReceive);
    }

    private static bool TryGetEvent<T>(out Action<ulong, T> eventAction) where T : struct
    {
        if (_eventStorage.TryGetValue(typeof(T), out var del))
        {
            eventAction = (Action<ulong, T>)del;
            return true;
        }

        eventAction = null;
        return false;
    }

    private static string GetEventName<T>() where T : struct
    {
        if (_eventNameLookup.TryGetValue(typeof(T), out var name))
            return name;

        name = $"{PREFIX}{typeof(T).Name}";

        _eventNameLookup.Add(typeof(T), name);

        return name;
    }

    
}
