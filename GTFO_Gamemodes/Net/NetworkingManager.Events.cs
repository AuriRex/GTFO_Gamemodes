using BepInEx.Unity.IL2CPP.Utils;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Mode;
using Gamemodes.Net.Packets;
using Player;
using SNetwork;
using System;
using System.Collections;
using UnityEngine;
using static Player.PlayerAgent;

namespace Gamemodes.Net;

public partial class NetworkingManager
{
    public static event Action<PlayerWrapper, int> OnPlayerChangedTeams;
    public static event Action<string> DoSwitchModeReceived;
    public static event Action<string> OnRemoteModeInstalledReported;

    private static void RegisterAllEvents()
    {
        RegisterEvent<pJoinInfo>(OnJoinInfoReceived);
        RegisterEvent<pInstalledMode>(OnInstalledModeReceived);
        RegisterEvent<pSwitchMode>(OnModeSwitchReceived);
        RegisterEvent<pForcedTeleport>(OnForcedTeleportReceived);
        RegisterEvent<pSpectatorSwitch>(OnSpectatorPacketReceived);
        RegisterEvent<pSetTeam>(OnSetTeamReceived);
        RegisterEvent<pWelcome>(OnWelcomeReceived);
        RegisterEvent<pChatLogMessage>(OnChatLogReceived);
    }

    public static void PostChatLog(string message)
    {
        if (message.Length > pChatLogMessage.MAX_LENGTH)
        {
            message = message.Substring(0, pChatLogMessage.MAX_LENGTH);
            Plugin.L.LogDebug($"{nameof(PostChatLog)} message longer than {pChatLogMessage.MAX_LENGTH} characters, truncating!");
        }

        var data = new pChatLogMessage()
        {
            Content = message,
        };

        SendEvent(data, invokeLocal: true);
    }

    private static void OnChatLogReceived(ulong senderId, pChatLogMessage data)
    {
        Plugin.PostLocalMessage(data.Content);
    }

    public static void AssignTeam(SNet_Player target, int teamId)
    {
        var data = new pSetTeam
        {
            PlayerID = target.Lookup,
            Team = teamId,
        };

        SendEvent(data, invokeLocal: true);
    }

    private static void OnSetTeamReceived(ulong senderId, pSetTeam data)
    {
        GetPlayerInfo(data.PlayerID, out var target);

        target.Team = data.Team;

        Plugin.L.LogDebug($"Player {target.NickName} ({target.ID}) switched teams to {data.Team}.");

        if (InLevel && GamemodeManager.CurrentSettings.UseTeamVisibility)
        {
            RefreshPlayerGhostsAndMarkers();
        }

        OnPlayerChangedTeams?.Invoke(target, data.Team);
    }

    private static void RefreshPlayerGhostsAndMarkers()
    {
        foreach(var playerInfo in AllValidPlayers)
        {
            SetPlayerGhostAndMarker(playerInfo);
        }
    }

    private static void SetPlayerGhostAndMarker(PlayerWrapper target)
    {
        if (!target.HasAgent || target.IsLocal)
            return;

        if (target.PlayerAgent == null || target.PlayerAgent.PlayerSyncModel == null || target.PlayerAgent.NavMarker == null)
            return;

        // Refreshes ghost team visibility
        target.PlayerAgent.PlayerSyncModel.GhostEnabled = true;

        if (target.PlayerAgent.NavMarker != null)
            target.PlayerAgent.NavMarker.SetMarkerVisible(target.CanBeSeenByLocalPlayer());
    }

    private static void OnSpectatorPacketReceived(ulong senderId, pSpectatorSwitch data)
    {
        GetPlayerInfo(senderId, out var sender);

        sender.IsSpectator = data.WantsToSpectate;
    }

    public static bool SendForceTeleport(SNet_Player target, Vector3 targetPos, Vector3 targetLookDir, eDimensionIndex dimension, WarpOptions options)
    {
        if (!SNet.IsMaster)
            return false;

        if (!GamemodeManager.CurrentAllowsForcedTP)
            return false;

        var data = new pForcedTeleport
        {
            DimensionIndex = (byte)dimension,
            WarpOptions = (byte)options,

            PosX = targetPos.x,
            PosY = targetPos.y,
            PosZ = targetPos.z,

            DirX = targetLookDir.x,
            DirY = targetLookDir.y,
            DirZ = targetLookDir.z,
        };

        if (target.Lookup == LocalPlayerId)
        {
            OnForcedTeleportReceived(LocalPlayerId, data);
            return true;
        }

        SendEvent(data, target);
        return true;
    }

    private static void OnForcedTeleportReceived(ulong senderId, pForcedTeleport data)
    {
        GetPlayerInfo(senderId, out var sender);

        if (!sender.IsMaster)
            return;

        if (!InLevel)
            return;

        if (!GamemodeManager.CurrentAllowsForcedTP)
            return;

        var pos = new Vector3(data.PosX, data.PosY, data.PosZ);
        var lookDir = new Vector3(data.DirX, data.DirY, data.DirZ);
        var dim = (eDimensionIndex)data.DimensionIndex;

        WarpOptions options = (WarpOptions)data.WarpOptions;

        Plugin.L.LogDebug($"{OnForcedTeleportReceived}: {data}");
        PlayerManager.GetLocalPlayerAgent()?.TryWarpTo(dim, pos, lookDir, options);
    }

    internal static void SendWelcome(SNet_Player target)
    {
        SendEvent(new pWelcome(), target);
    }

    private static void OnWelcomeReceived(ulong senderId, pWelcome _)
    {
        GetPlayerInfo(senderId, out var info);

        if (!info.IsMaster)
            return;

        SendJoinInfo();

        OnJoinedLobbySyncEvent?.Invoke(info);
    }

    internal static void SendJoinInfo()
    {
        SendEventAndInvokeLocally(new pJoinInfo
        {
            Major = Plugin.Version.Major,
            Minor = Plugin.Version.Minor,
            Patch = Plugin.Version.Patch,
        });

        foreach (var modeId in GamemodeManager.LoadedModeIds)
        {
            SendEventAndInvokeLocally(new pInstalledMode
            {
                GamemodeID = modeId,
            });
        }
    }

    private static void OnJoinInfoReceived(ulong senderId, pJoinInfo data)
    {
        GetPlayerInfo(senderId, out var info);

        info.LoadedVersion = new PrimitiveVersion(data.Major, data.Minor, data.Patch);

        if (!info.VersionMatches)
        {
            Plugin.L.LogWarning($"Version mismatch for player \"{info.NickName}\" ({senderId}), {info.LoadedVersion} != {Plugin.VERSION}");
            return;
        }

        Plugin.L.LogMessage($"Compatible player \"{info.NickName}\" ({senderId}) join info received!");
    }

    private static void OnInstalledModeReceived(ulong senderId, pInstalledMode data)
    {
        GetPlayerInfo(senderId, out var info);

        info.ReportedInstalledGamemodes.Add(data.GamemodeID);

        Plugin.L.LogDebug($"Player \"{info.NickName}\" ({senderId}) has mode \"{data.GamemodeID}\" installed.");

        if (IsLocalSender(senderId))
            return;

        OnRemoteModeInstalledReported?.Invoke(data.GamemodeID);
    }

    public static void SendSwitchModeTo(string gamemodeId, SNet_Player target)
    {
        if (!SNet.IsMaster)
            return;

        SendEvent(new pSwitchMode
        {
            GamemodeID = gamemodeId,
        }, target);
    }

    public static void SendSwitchModeAll(string gamemodeId)
    {
        if (!SNet.IsMaster)
            return;

        SendEventAndInvokeLocally(new pSwitchMode
        {
            GamemodeID = gamemodeId,
        });
    }

    private static void OnModeSwitchReceived(ulong senderId, pSwitchMode data)
    {
        GetPlayerInfo(senderId, out var info);

        if (!info.IsMaster)
            return;

        DoSwitchModeReceived?.Invoke(data.GamemodeID);
    }
}
