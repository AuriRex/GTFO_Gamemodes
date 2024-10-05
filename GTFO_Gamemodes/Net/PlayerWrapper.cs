using Gamemodes.Mode;
using Il2CppSystem.Reflection;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using UnityEngine;
using static Player.PlayerAgent;

namespace Gamemodes.Net;

public record class PlayerWrapper
{
    public PlayerWrapper(ulong playerId)
    {
        ID = playerId;
        NetworkingManager.TryGetSender(playerId, out var player);
        NetPlayer = player;
        IsBot = player?.IsBot ?? false;
    }

    public bool ValidPlayer => HasAgent && !IsSpectator;

    public ulong ID { get; init; }

    public SNet_Player NetPlayer { get; private set; }

    public bool IsMaster => NetPlayer.IsMaster;

    public bool HasAgent => NetPlayer.HasPlayerAgent;

    public PlayerAgent PlayerAgent => NetPlayer?.PlayerAgent?.TryCast<PlayerAgent>();

    public string NickName => NetPlayer.NickName;

    public bool IsSpectator { get; set; }

    public PrimitiveVersion LoadedVersion { get; internal set; } = PrimitiveVersion.None;

    public bool VersionMatches => LoadedVersion == Plugin.Version || IsBot;

    public HashSet<string> ReportedInstalledGamemodes { get; init; } = new();

    public int Team { get; internal set; }
    public bool IsBot { get; internal set; } = false;
    public bool IsLocal => NetPlayer.IsLocal;
    public string PlayerColorTag => $"<#{ColorUtility.ToHtmlStringRGB(NetPlayer.PlayerColor)}>";

    public bool HasModeInstalled(string modeId)
    {
        if (IsBot)
            return true;
        
        return ReportedInstalledGamemodes.Contains(modeId);
    }

    public bool WarpTo(Vector3 pos, Vector3 lookDir, eDimensionIndex dimension, WarpOptions options)
    {
        return NetworkingManager.SendForceTeleport(NetPlayer, pos, lookDir, dimension, options);
    }

    public bool IsOnSameTeamAs(PlayerWrapper other)
    {
        return Team == other.Team;
    }

    public bool CanBeSeenByLocalPlayer()
    {
        return TeamVisibility.LocalPlayerCanSeeTeam(Team);
    }
}
