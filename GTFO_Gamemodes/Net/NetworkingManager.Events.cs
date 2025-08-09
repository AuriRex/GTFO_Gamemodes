using Gamemodes.Core;
using Gamemodes.Net.Packets;
using Player;
using SNetwork;
using System;
using System.Linq;
using AIGraph;
using Gamemodes.Patches.Required;
using UnityEngine;
using static Player.PlayerAgent;

namespace Gamemodes.Net;

public partial class NetworkingManager
{
    public static event Action<pRayCast> OnRayCastInstructionsReceived;
    public static event Action<PlayerWrapper, pGearChangeNotif> OnPlayerChangedGear;
    public static event Action<PlayerWrapper, int> OnPlayerChangedTeams;
    public static event Action<string> DoSwitchModeReceived;
    public static event Action<string> OnRemoteModeInstalledReported;

    private static void RegisterAllEvents()
    {
        RegisterEventInternal<pJoinInfo>(OnJoinInfoReceived);
        RegisterEventInternal<pInstalledMode>(OnInstalledModeReceived);
        RegisterEventInternal<pSwitchMode>(OnModeSwitchReceived);
        RegisterEventInternal<pForcedTeleport>(OnForcedTeleportReceived);
        RegisterEventInternal<pSpectatorSwitch>(OnSpectatorPacketReceived);
        RegisterEventInternal<pSetTeam>(OnSetTeamReceived);
        RegisterEventInternal<pWelcome>(OnWelcomeReceived);
        RegisterEventInternal<pChatLogMessage>(OnChatLogReceived);
        RegisterEventInternal<pSpawnItemInLevel>(OnSpawnItemInLevelReceived);
        RegisterEventInternal<pSpawnItemForPlayer>(OnSpawnItemForPlayerReceived);
        RegisterEventInternal<pGearChangeNotif>(OnGearChangeNotifReceived);
        RegisterEventInternal<pRayCast>(OnRayCastPacketReceived);
        RegisterEventInternal<pHiIHasArrived>(OnIHasArrivedReceived);
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

    public static void AssignTeamCatchup(SNet_Player target, int teamId, SNet_Player catchupTarget)
    {
        if (!SNet.IsMaster)
            return;
        
        var data = new pSetTeam
        {
            PlayerID = target.Lookup,
            Team = teamId,
        };

        SendEvent(data, targetPlayer: catchupTarget, invokeLocal: false);
    }

    private static void OnSetTeamReceived(ulong senderId, pSetTeam data)
    {
        GetPlayerInfo(data.PlayerID, out var target);

        target.Team = data.Team;

        Plugin.L.LogDebug($"Player {target.NickName} ({target.ID}) switched teams to {data.Team}.");

        if (target.IsLocal)
        {
            LocalPlayerTeam = data.Team;
        }
        
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

        Plugin.L.LogDebug($"{nameof(OnForcedTeleportReceived)}: {data}");
        PlayerManager.GetLocalPlayerAgent()?.TryWarpTo(dim, pos, lookDir, options);
    }

    internal static void SendWelcome(SNet_Player target)
    {
        SendEvent(new pWelcome(), target, channelType: SNet_ChannelType.SessionOrderCritical);
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
        Plugin.L.LogDebug($"Sending join info, v{Plugin.Version} and {GamemodeManager.LoadedModeIds.Count()} installed gamemodes.");
        SendEventAndInvokeLocally(new pJoinInfo
        {
            Major = Plugin.Version.Major,
            Minor = Plugin.Version.Minor,
            Patch = Plugin.Version.Patch,
        }, targetPlayer: SNet.Master, channelType: SNet_ChannelType.SessionOrderCritical);

        foreach (var modeId in GamemodeManager.LoadedModeIds)
        {
            SendEventAndInvokeLocally(new pInstalledMode
            {
                GamemodeID = modeId,
            }, targetPlayer: SNet.Master, channelType: SNet_ChannelType.SessionOrderCritical);
        }
    }

    private static void OnJoinInfoReceived(ulong senderId, pJoinInfo data)
    {
        GetPlayerInfo(senderId, out var info);

        info.LoadedVersion = new PrimitiveVersion(data.Major, data.Minor, data.Patch);

        Plugin.L.LogMessage($"Received join info packed from \"{info.NickName}\" ({senderId})");
        
        if (!info.VersionMatches)
        {
            Plugin.L.LogWarning($"Version mismatch for player \"{info.NickName}\" ({senderId}), {info.LoadedVersion} != {Plugin.VERSION}");
        }
        
        Plugin.L.LogDebug($" ^ PlayerWrapper: {info}");
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
        }, target, channelType: SNet_ChannelType.SessionOrderCritical);
    }

    public static void SendSwitchModeAll(string gamemodeId)
    {
        if (!SNet.IsMaster)
            return;

        SendEventAndInvokeLocally(new pSwitchMode
        {
            GamemodeID = gamemodeId,
        }, channelType: SNet_ChannelType.SessionOrderCritical);
    }

    private static void OnModeSwitchReceived(ulong senderId, pSwitchMode data)
    {
        GetPlayerInfo(senderId, out var info);

        if (!info.IsMaster)
            return;

        DoSwitchModeReceived?.Invoke(data.GamemodeID);
    }

    public static void SendSpawnItemInLevel(AIG_CourseNode node, Vector3 position, uint itemId, float ammoMultiplier = 1.0f)
    {
        if (!SNet.IsMaster)
            return;
        
        var data = new pSpawnItemInLevel
        {
            ItemID = itemId,
            AmmoMultiplier = ammoMultiplier,
            Position = position,
            Node = node,
            ReplicatorKey = (ushort) ReplicationPatch.HighestSlotUsed_SelfManaged
        };
        
        SendEvent(data, invokeLocal: true);
    }

    private static void OnSpawnItemInLevelReceived(ulong senderId, pSpawnItemInLevel data)
    {
        GetPlayerInfo(senderId, out var senderInfo);

        if (!senderInfo.IsMaster)
            return;

        var success =
            SpawnUtils.SpawnItemLocally(data.ItemID, data.Node, data.Position, out var item, data.AmmoMultiplier, data.ReplicatorKey);
        
        Plugin.L.LogDebug($"{nameof(OnSpawnItemInLevelReceived)}: Spawned Item?: {success} | Item: {item?.ItemDataBlock?.publicName}");
    }
    
    public static void SendSpawnItemForPlayer(SNet_Player target, uint itemId, float ammoMultiplier = 1.0f,
        bool doWield = false)
    {
        if (!SNet.IsMaster)
            return;
        
        var data = new pSpawnItemForPlayer
        {
            PlayerID = target?.Lookup ?? 0,
            ItemID = itemId,
            AmmoMultiplier = ammoMultiplier,
            DoWield = doWield,
            ReplicatorKey = (ushort) ReplicationPatch.HighestSlotUsed_SelfManaged
        };
        
        SendEvent(data, invokeLocal: true);
    }
    
    private static void OnSpawnItemForPlayerReceived(ulong sender, pSpawnItemForPlayer data)
    {
        GetPlayerInfo(sender, out var senderInfo);

        if (!senderInfo.IsMaster)
            return;

        PlayerWrapper targetPlayer = null;
        
        if (data.PlayerID != 0)
            GetPlayerInfo(data.PlayerID, out targetPlayer);
        
        var success = SpawnUtils.SpawnItemAndPickUp(data.ItemID, targetPlayer, data.AmmoMultiplier, data.DoWield, data.ReplicatorKey);
        
        Plugin.L.LogDebug($"{nameof(OnSpawnItemForPlayerReceived)}: Spawned Item for player {targetPlayer?.NickName} ({data.PlayerID}): {success}");
    }

    public static void SendLocalPlayerGearChanged(uint checksum, uint previousChecksum, InventorySlot slot)
    {
        var data = new pGearChangeNotif
        {
            gearChecksumPrevious = previousChecksum,
            gearChecksum = checksum,
            isGun = slot == InventorySlot.GearSpecial | slot == InventorySlot.GearStandard,
            isTool = slot == InventorySlot.GearClass,
        };
        
        SendEvent(data, invokeLocal: true);
    }
    
    private static void OnGearChangeNotifReceived(ulong sender, pGearChangeNotif data)
    {
        if (!SNet.IsMaster)
            return;

        GetPlayerInfo(sender, out var info);

        OnPlayerChangedGear?.Invoke(info, data);
    }

    public static void SendRayCastInstructions(byte type, Vector3 origin, Vector3 direction, SNet_Player targetPlayer = null)
    {
        if (!SNet.IsMaster)
            return;
        
        var data = new pRayCast
        {
            Type = type,
            Origin = origin,
            Direction = direction,
        };
        
        SendEvent(data, targetPlayer, invokeLocal: true);
    }
    
    private static void OnRayCastPacketReceived(ulong sender, pRayCast data)
    {
        GetPlayerInfo(sender, out var info);

        if (!info.IsMaster)
            return;

        OnRayCastInstructionsReceived?.Invoke(data);
    }
    
    internal static void SendIHasArrived()
    {
        var data = new pHiIHasArrived
        {
            hi = 1,
        };
        
        SendEvent(data, SNet.Master, invokeLocal: true);
    }
    
    private static void OnIHasArrivedReceived(ulong sender, pHiIHasArrived _)
    {
        if (!SNet.Master)
            return;
        
        GetPlayerInfo(sender, out var info);

        SyncData(info);
        
        GamemodeManager.OnRemotePlayerEnteredLevel(info);
    }
}
