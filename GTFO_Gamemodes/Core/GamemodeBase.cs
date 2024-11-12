using Gamemodes.Net;
using Player;
using System.Collections.Generic;
using Gamemodes.Patches.Required;
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

    public virtual void OnPlayerCountChanged()
    {

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

        localPlayer.Damage.AddHealth(localPlayer.Damage.HealthMax, localPlayer);
    }
}
