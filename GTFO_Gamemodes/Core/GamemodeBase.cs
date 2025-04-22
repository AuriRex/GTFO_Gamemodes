using Gamemodes.Net;
using Player;
using System.Collections.Generic;
using System.Linq;
using Gamemodes.Net.Packets;
using Gamemodes.Patches.Required;
using Gear;
using SNetwork;
using UnityEngine;

namespace Gamemodes.Core;

public abstract class GamemodeBase
{
    public abstract string ID { get; }

    public abstract string DisplayName { get; }
    public virtual string SubTitle { get; } = string.Empty;
    public virtual string Description { get; } = string.Empty;
    public virtual Sprite SpriteSmall { get; } = null;
    public virtual Sprite SpriteLarge { get; } = null;

    public GameObject gameObject;
    
    public abstract ModeSettings Settings { get; }

    public TeamVisibility TeamVisibility { get; protected set; } = new();
    public CustomCommands ChatCommands { get; protected set; } = new();

    public IEnumerable<PlayerWrapper> ValidPlayers => NetworkingManager.AllValidPlayers;
    public IEnumerable<PlayerWrapper> Spectators => NetworkingManager.Spectators;

    public NetEvents Net { get; } = new();

    public virtual void Init()
    {

    }

    public virtual void Enable()
    {

    }

    public virtual void Disable()
    {

    }

    public virtual Color? GetElevatorColor()
    {
        return null;
    }
    
    public virtual void OnPlayerCountChanged()
    {

    }

    public virtual void OnRemotePlayerEnteredLevel(PlayerWrapper player)
    {
        
    }
    
    public virtual void OnPlayerChangedGear(PlayerWrapper player, pGearChangeNotif data)
    {
        if (!SNet.IsMaster)
            return;
        
        if (!data.isTool)
            return;

        var previousTool = GearManager.GetAllPlayerGear().FirstOrDefault(gear => gear.GetCustomChecksum() == data.gearChecksumPrevious);

        if (previousTool == null)
            return;

        if (previousTool.PublicGearName.Contains("Krieger O4"))
        {
            Plugin.L.LogDebug($"Cleaning up mines for player {player.NickName}");
            GearUtils.CleanupMinesForPlayer(player);
        }
        
        if (previousTool.PublicGearName.Contains("Stalwart Flow G2"))
        {
            Plugin.L.LogDebug($"Cleaning up cfoam blobs for player {player.NickName}");
            GearUtils.CleanupGlueBlobsForPlayer(player);
        }
    }

    public static void SetPushForceMultiplierForLocalPlayer(float pushForceMultiplier, float slidePushForceMultiplier)
    {
        PushForcePatch.PushForceMultiplier = pushForceMultiplier;
        PushForcePatch.SlidePushForceMultiplier = slidePushForceMultiplier;
    }
    
    public static void PostLocalChatMessage(string msg)
    {
        Plugin.PostLocalMessage(msg);
    }

    public static void InstantReviveLocalPlayer()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return;

        var ploc = localPlayer.Locomotion;

        if (ploc.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed)
        {
            ploc.ChangeState(PlayerLocomotion.PLOC_State.Stand, wasWarpedIntoState: false);
        }

        localPlayer.Sync.SendLocomotion(ploc.m_currentStateEnum, localPlayer.transform.position, localPlayer.FPSCamera.Forward, 0f, 0f);
        
        localPlayer.Damage.AddHealth(localPlayer.Damage.HealthMax, localPlayer);
    }
}
