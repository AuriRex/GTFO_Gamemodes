
using System.Linq;
using ExtraModes.Net;
using ExtraModes.Net.Packets;
using Gamemodes;
using Gamemodes.Core;
using Gamemodes.Extensions;
using Gamemodes.Net;
using Gamemodes.Net.Packets;
using Il2CppInterop.Runtime.Injection;
using Player;
using SNetwork;
using UnityEngine;

namespace ExtraModes.GeoGuesser;

public class GGMode : GamemodeBase
{
    public const string MODE_ID = "geo_guesser_thing";
    
    private static NetBoxManager _boxManager;
    private static RayManager _rayManager;
    
    public override string ID => MODE_ID;
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
        MapIconsToReveal = Utils.EVERYTHING_EXCEPT_LOCKERS,
    };
    
    // Raycast -> collider
    // Create boxes

    public override void Init()
    {
        _boxManager = new NetBoxManager(Net);

        ChatCommands
            .Add("box", CreateBox)
            .Add("ray", CastRayThing)
            .Add("start", StartGame)
            .Add("stop", StopGame);

        TeamVisibility.Team(GGTeams.HiddenOne).CanSeeSelf().And(GGTeams.Seekers);
        //TeamVisibility.Team(GGTeams.Seekers).CanSeeSelf();

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<GGUpdaterComp>())
        {
            ClassInjector.RegisterTypeInIl2Cpp<GGUpdaterComp>();
        }
    }

    private static string CastRayThing(string[] args)
    {
        if (!SNet.IsMaster)
            return "Host only!";
        
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return "Error :c";

        var cam = localPlayer.FPSCamera;
        
        NetworkingManager.SendRayCastInstructions(0, cam.Position, cam.Forward);

        return "Casting ray.";
    }

    public static bool IsGameActive { get; internal set; } = false;
    
    private static string StartGame(string[] args)
    {
        if (!SNet.IsMaster)
            return "Host only!";
        
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player.IsLocallyOwned)
            {
                player.gameObject.GetOrAddComponent<GGUpdaterComp>();
                continue;
            }
            
            NetworkingManager.AssignTeam(player.Owner, (int) GGTeams.Seekers);
        }

        IsGameActive = true;
        
        return "Round started!";
    }
    
    private static string StopGame(string[] args)
    {
        if (!SNet.IsMaster)
            return "Host only!";
        
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player.IsLocallyOwned)
            {
                player.gameObject.GetComponent<GGUpdaterComp>().SafeDestroy();
            }
            
            NetworkingManager.AssignTeam(player.Owner, (int) GGTeams.HiddenOne);
        }

        //_rayManager.Reset();
        NetworkingManager.SendRayCastInstructions(1, Vector3.zero, Vector3.up);
        
        IsGameActive = false;
        
        return "Round stopped!";
    }

    private static string CreateBox(string[] args)
    {
        if (!SNet.IsMaster)
            return "Host only!";

        /*var boxAction = NetBoxManager.BoxAction.CreateOrReposition;
        
        if (args.FirstOrDefault(arg => arg == "invis") != null)
            boxAction = NetBoxManager.BoxAction.CreateOrRepositionButInvisible;
            */

        var invis = args.FirstOrDefault(arg => arg == "invis") != null;
        
        var boxType = NetBoxManager.BoxType.Floor1X1;
        
        if (args.FirstOrDefault(arg => arg == "wall") != null)
            boxType = NetBoxManager.BoxType.Wall1X1;
        
        if (args.FirstOrDefault(arg => arg == "wall2") != null)
            boxType = NetBoxManager.BoxType.Wall1X1_TWO;
        
        var localPlayer = PlayerManager.GetLocalPlayerAgent();
        
        _boxManager.CreateBox(localPlayer.Position, boxType, invis);

        return "Box created?";
    }

    public override void Enable()
    {
        NetworkingManager.OnRayCastInstructionsReceived += OnRayCastInstructionsReceived;
        GameEvents.OnGameStateChanged += OnGameStateChanged;
        
        _rayManager = new();
    }
    
    public override void Disable()
    {
        NetworkingManager.OnRayCastInstructionsReceived -= OnRayCastInstructionsReceived;
        GameEvents.OnGameStateChanged -= OnGameStateChanged;
        
        _rayManager.Dispose();
        _rayManager = null;
    }

    public override void OnRemotePlayerEnteredLevel(PlayerWrapper player)
    {
        _boxManager.MasterHandleLateJoiner(player);
        _rayManager.MasterHandleLateJoiner(player);
        
        if (!IsGameActive)
            NetworkingManager.AssignTeam(player, (int) GGTeams.HiddenOne);
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