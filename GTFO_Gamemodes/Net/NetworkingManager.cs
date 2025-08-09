using Gamemodes.Core;
using GTFO.API;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using Player;
using Steamworks;

namespace Gamemodes.Net;

public partial class NetworkingManager
{
    private const string PREFIX = "CGamemodes_";

    private static readonly Dictionary<Type, string> _eventNameLookup = new();
    private static readonly Dictionary<Type, Delegate> _eventStorage = new();

    private static readonly Dictionary<ulong, PlayerWrapper> _syncedPlayers = new();

    public static IEnumerable<PlayerWrapper> SyncedPlayers => _syncedPlayers.Values;

    public static IEnumerable<PlayerWrapper> AllValidPlayers => SyncedPlayers.Where(p => p.ValidPlayer);

    public static IEnumerable<PlayerWrapper> Spectators => SyncedPlayers.Where(p => p.IsSpectator);

    public static ulong LocalPlayerId => SNet.LocalPlayer?.Lookup ?? 0;
    public static int LocalPlayerTeam { get; private set; }

    public static bool AllPlayersVersionMatches => SyncedPlayers.All(pi => pi.VersionMatches);

    public static bool AllPlayersHaveModeInstalled => SyncedPlayers.All(pi => pi.HasModeInstalled(GamemodeManager.CurrentMode?.ID));

    public static bool InLevel => GameStateManager.CurrentStateName == eGameStateName.InLevel;

    /// <summary>
    /// PlayerWrapper => Master
    /// </summary>
    public static event Action<PlayerWrapper> OnJoinedLobbySyncEvent;
    public static event Action OnPlayerCountChanged;

    internal static void Init()
    {
        GameEvents.OnGameDataInit += OnGameDataInit;
        GameEvents.OnGameStateChanged += OnGameStateChanged;
    }

    private static void OnGameDataInit()
    {
        Plugin.L.LogDebug("Registering base events ...");
        RegisterAllEvents();

        // SNet_Events.OnPlayerEvent += (Action<SNet_Player, SNet_PlayerEvent, SNet_PlayerEventReason>)OnPlayerEvent;
        // SNet_Events.OnPlayerJoin += (Action)OnPlayerJoined;
        // SNet_Events.OnPlayerLeave += (Action)OnPlayerCountChangedImpl;
    }
    
    private static void OnGameStateChanged(eGameStateName state)
    {
        Plugin.L.LogDebug($"{nameof(NetworkingManager)} OnGameStateChanged(): {state}");
        switch (state)
        {
            case eGameStateName.Lobby:
                OnGameStateLobby();
                break;
            case eGameStateName.InLevel:
                SendIHasArrived();
                break;
        }
    }
    
    private static void OnGameStateLobby()
    {
        if (SNet.IsMaster)
            return;

        SendJoinInfo();

        if (!SNet.IsInLobby)
            return;
        
        var lobbyID = new CSteamID(SNet.Lobby.Identifier.ID);
        var modeString = SteamMatchmaking.GetLobbyData(lobbyID, GamemodeManager.STEAM_CUSTOM_GAMEMODE_PCH_KEY);

        if (string.IsNullOrWhiteSpace(modeString))
        {
            Plugin.L.LogWarning("No mode string found in steam lobby settings.");
            return;
        }
        
        DoSwitchModeReceived?.Invoke(modeString);
    }
    
    private static void OnPlayerJoined(SNet_Player newPlayer)
    {
        if (newPlayer == null)
            return;

        if (!GetPlayerInfo(newPlayer.Lookup, out var info))
            return; // Player already in lobby
        
        Plugin.L.LogDebug($"{info.NickName} has joined!");
        if (SNet.IsMaster)
        {
            PostChatLog($"<#0F0>>> <color=orange>{info.NickName}</color> has connected.</color>");
        }
        SendWelcome(newPlayer);
        SendSwitchModeTo(GamemodeManager.CurrentMode.ID, newPlayer);
        
        OnPlayerCountChangedImpl();
    }

    private static void OnPlayerCountChangedImpl()
    {
        Plugin.L.LogDebug($"OnPlayerCountChanged");
        OnPlayerCountChanged?.Invoke();
        GamemodeManager.OnPlayerCountChanged();
    }
    
    public static void OnPlayerAddedToSession(SNet_Player player)
    {
        OnPlayerJoined(player);
    }
    
    public static void OnPlayerRemovedFromSession(SNet_Player player)
    {
        if (!_syncedPlayers.Remove(player.Lookup))
            return;

        Plugin.L.LogDebug($"Removed \"{player.NickName}\" from _syncedPlayers.");
        
        OnPlayerCountChangedImpl();
        
        if (!SNet.IsMaster)
            return;
        
        PostChatLog($"<#F00><< <color=orange>{player.NickName}</color> has disconnected.</color>");
    }
    
    private static void OnPlayerEvent(SNet_Player player, SNet_PlayerEvent playerEvent, SNet_PlayerEventReason reason)
    {
        Plugin.L.LogDebug($"OnPlayerEvent: {player.NickName}, event: {playerEvent}, reason: {reason}");
    }

    internal static void CleanupPlayers()
    {
        HashSet<ulong> connectedPlayers = new();
        foreach (var player in SNet.LobbyPlayers)
        {
            connectedPlayers.Add(player.Lookup);
        }
        
        foreach (var playerAgent in PlayerManager.PlayerAgentsInLevel)
        {
            if (playerAgent?.Owner == null || playerAgent.m_isBeingDestroyed)
                continue;
            
            connectedPlayers.Add(playerAgent.Owner.Lookup);
        }

        var disconnectedPlayers = _syncedPlayers.Values.Where(p => !connectedPlayers.Contains(p.ID)).ToArray();

        if (disconnectedPlayers.Length == 0)
            return;

        Plugin.L.LogWarning($"Cleanup: Removing {disconnectedPlayers.Length} players");
        foreach (var player in disconnectedPlayers)
        {
            Plugin.L.LogDebug($"Removing \"{player.NickName}\" from _syncedPlayers.");
            _syncedPlayers.Remove(player.ID);

            if (player.IsBot)
                continue;

            if (!SNet.IsMaster)
                return;
            
            PostChatLog($"<#F00><< <color=orange>{player.NickName}</color> has disconnected.</color>");
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

        _syncedPlayers.Remove(playerId);

        playerInfo = new(playerId);
        Plugin.L.LogWarning($"Synced: {playerId}: {playerInfo}");
        _syncedPlayers.Add(playerId, playerInfo);
        return true;
    }

    public static bool GetPlayerInfo(SNet_Player player, out PlayerWrapper playerInfo, bool forceReset = false)
    {
        if (player == null)
            throw new ArgumentException("player is null", nameof(player));
        return GetPlayerInfo(player.Lookup, out playerInfo, forceReset);
    }

    public static PlayerWrapper GetLocalPlayerInfo()
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

    public static void SendEventAndInvokeLocally<T>(T data, SNet_Player targetPlayer = null, SNet_ChannelType channelType = SNet_ChannelType.GameOrderCritical) where T : struct
    {
        SendEvent(data, targetPlayer, invokeLocal: true, channelType);
    }

    public static void SendEvent<T>(T data, SNet_Player targetPlayer = null, bool invokeLocal = false, SNet_ChannelType channelType = SNet_ChannelType.GameOrderCritical) where T : struct
    {
        var eventName = GetEventName<T>();

        if (!NetworkAPI.IsEventRegistered(eventName))
            throw new ArgumentException($"The provided type \"{typeof(T).Name}\" has not been registered as a valid event. ({eventName})", nameof(data));

        if (targetPlayer == null)
        {
            NetworkAPI.InvokeEvent(eventName, data, channelType);
        }
        else
        {
            NetworkAPI.InvokeEvent(eventName, data, targetPlayer, channelType);
        }

        if (!invokeLocal)
            return;

        if (targetPlayer?.IsLocal ?? false)
            return;
        
        if (!TryGetEvent<T>(out var eventAction))
            return;

        eventAction.Invoke(LocalPlayerId, data);
    }

    private static void EventInvoker<T>(ulong sender, T data) where T : struct
    {
        var net = GamemodeManager.CurrentMode.Net;

        if (!net.HasEvent<T>())
            return;

        net.InvokeEvent(sender, data);
    }
    
    internal static void RegisterEventWrapped<T>() where T : struct
    {
        var eventName = GetEventName<T>();

        if (NetworkAPI.IsEventRegistered(eventName))
            return;
        
        _eventStorage.Add(typeof(T), EventInvoker<T>);
        
        NetworkAPI.RegisterEvent<T>(eventName, EventInvoker<T>);
    }
    
    private static void RegisterEventInternal<T>(Action<ulong, T> onReceive) where T : struct
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

    private static void SyncData(PlayerWrapper playerToSyncTo)
    {
        if (!SNet.IsMaster)
            return;
        
        // Sync current team states
        foreach (var (_, player) in _syncedPlayers)
        {
            if (player.ID == playerToSyncTo.ID)
                continue;
            
            AssignTeamCatchup(player, player.Team, playerToSyncTo);
        }
    }
}
