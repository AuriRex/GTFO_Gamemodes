
using System.Linq;
using ExtraModes.Net;
using ExtraModes.Net.Packets;
using Gamemodes;
using Gamemodes.Core;
using Gamemodes.Net;
using Gamemodes.Net.Packets;
using Player;
using SNetwork;
using UnityEngine;

namespace ExtraModes.GeoGuesser;

public class GGMode : GamemodeBase
{
    private static NetBoxManager _boxManager;
    public override string ID => "geo_guesser_thing";
    public override string DisplayName => "Geo Guesser + Secret Room";
    public override string Description => "TODO";

    public override ModeSettings Settings => new()
    {
        AllowForcedTeleportation = true,
        InfiniteBackpackAmmo = true,
        InfiniteSentryAmmo = true,
        UseTeamVisibility = true,
        RevealEntireMap = true,
        BlockWorldEvents = true,
        RemoveTerminalCommands = true,
        RemoveCheckpoints = true,
        RemoveBloodDoors = true,
        OpenAllSecurityDoors = true,
        OpenAllWeakDoors = true,
        DisableVoiceLines = true,
        PreventDefaultFailState = true,
        PreventExpeditionEnemiesSpawning = true,
        PreventRespawnRoomsRespawning = true,
    };
    
    // Raycast -> collider
    // Create boxes

    public override void Init()
    {
        _boxManager = new NetBoxManager(Net);

        ChatCommands.Add("box", CreateBox);
    }

    private static string CreateBox(string[] args)
    {
        if (!SNet.IsMaster)
            return "Host only.";

        var boxAction = NetBoxManager.BoxAction.CreateOrReposition;
        
        if (args.FirstOrDefault(arg => arg == "invis") != null)
            boxAction = NetBoxManager.BoxAction.CreateOrRepositionButInvisible;

        var boxType = NetBoxManager.BoxType.Floor1X1;
        
        if (args.FirstOrDefault(arg => arg == "wall") != null)
            boxType = NetBoxManager.BoxType.Wall1X1;
        
        if (args.FirstOrDefault(arg => arg == "wall2") != null)
            boxType = NetBoxManager.BoxType.Wall1X1_TWO;
        
        var localPlayer = PlayerManager.GetLocalPlayerAgent();
        
        _boxManager.CreateBox(localPlayer.Position, boxType);

        return "Box created?";
    }

    public override void Enable()
    {
        NetworkingManager.OnRayCastInstructionsReceived += OnRayCastInstructionsReceived;
        GameEvents.OnGameStateChanged += OnGameStateChanged;
    }
    
    public override void Disable()
    {
        NetworkingManager.OnRayCastInstructionsReceived -= OnRayCastInstructionsReceived;
        GameEvents.OnGameStateChanged -= OnGameStateChanged;
    }
    
    private void OnGameStateChanged(eGameStateName state)
    {
        switch (state)
        {
            case eGameStateName.Lobby:
                _boxManager.Cleanup();
                break;
        }
    }
    
    private void OnRayCastInstructionsReceived(pRayCast data)
    {
        
    }
}