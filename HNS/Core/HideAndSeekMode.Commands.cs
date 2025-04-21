using System;
using System.Linq;
using Gamemodes.Core;
using Gamemodes.Net;
using HNS.Components;
using HNS.Net;
using LevelGeneration;
using Player;
using SNetwork;
using UnityEngine;

namespace HNS.Core;

internal partial class HideAndSeekMode
{
    private static string SendHelpMessage(string[] arg)
    {
        PostLocalChatMessage(" ");
        PostLocalChatMessage("<align=center><color=orange><b><size=120%>Welcome to Hide and Seek!</align></color></b></size>");
        PostLocalChatMessage("---------------------------------------------------------------");
        PostLocalChatMessage("Use the <u>chat-commands</u> '<#f00>/seeker</color>' and '<#0ff>/hider</color>'");
        PostLocalChatMessage("to assign yourself to the two teams.");
        PostLocalChatMessage("---------------------------------------------------------------");
        PostLocalChatMessage("<#f00>Host only:</color>");
        PostLocalChatMessage("Use the command '<color=orange>/hnsstart</color>' to start the game.");
        PostLocalChatMessage("<#888>You can use '<color=orange>/hnsstop</color>' to end an active game at any time.</color>");
        PostLocalChatMessage("---------------------------------------------------------------");
        return string.Empty;
    }
    
    private static string StartGame(string[] args)
    {
        if (!SNet.IsMaster)
            return "Only the Master can start.";

        WarpAllPlayersToMaster();

        var seekers = NetworkingManager.AllValidPlayers.Where(pw => IsSeeker(pw.Team)).Select(pw => pw.ID)
            .ToArray();

        int? setupTime = null;
        
        if (args.Length >= 1)
        {
            if (int.TryParse(args[0], out var value) && value >= 1 && value <= 255)
            {
                setupTime = value;
            }
        }
        
        NetSessionManager.SendStartGamePacket(setupTime, seekers);

        return string.Empty;
    }

    private static string StopGame(string[] args)
    {
        if (!SNet.IsMaster)
            return "Only the Master can stop.";

        NetSessionManager.SendStopGamePacket();

        return string.Empty;
    }
    
    private static string AbortGame(string[] args)
    {
        if (!SNet.IsMaster)
            return "Only the Master can abort.";

        NetSessionManager.SendStopGamePacket(abortGame: true);

        return string.Empty;
    }

    private static string SwitchToLobby(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)LocalGetRealTeam(GMTeam.PreGame));
        return string.Empty;
    }

    private static string SwitchToHider(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)LocalGetRealTeam(GMTeam.Hiders));
        return string.Empty;
    }

    private static string SwitchToSeeker(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)LocalGetRealTeam(GMTeam.Seekers));
        return string.Empty;
    }
    
    private static string SwitchToSpectator(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.Camera);
        return string.Empty;
    }
    
    private static string AssignHideAndSeekTeam(string[] args)
    {
        if (NetSessionManager.HasSession)
            return "Can't change teams while a round is active.";
        
        if (args.Length == 0)
        {
            return $"You are currently on team {_localTeam}";
        }

        HNSTeam team;
        
        if (int.TryParse(args[0], out var value))
        {
            team = (HNSTeam) value;
        }
        else if(Enum.TryParse<HNSTeam>(args[0], ignoreCase: true, out var hnsTeam))
        {
            team = hnsTeam;
        }
        else
        {
            switch (args[0].ToLower())
            {
                case "leave":
                case "remove":
                case "clear":
                case "n":
                    team = HNSTeam.None;
                    break;
                case "a":
                    team = HNSTeam.Alpha;
                    break;
                case "b":
                    team = HNSTeam.Beta;
                    break;
                case "c":
                    team = HNSTeam.Gamma;
                    break;
                case "d":
                    team = HNSTeam.Delta;
                    break;
                default:
                    return "<#F00>Invalid team</color>";
            }
        }

        if (team < 0 || (int)team > 4)
            return "<#F00>Invalid team</color>";
        
        _localTeam = team;

        if (team == HNSTeam.None)
        {
            NetworkingManager.AssignTeam(SNet.LocalPlayer, (int) team);
            return "You left the team.";
        }
        
        var currentTeam = (GMTeam)NetworkingManager.LocalPlayerTeam;
        var realTeam = LocalGetRealTeam(currentTeam);
        if (realTeam != currentTeam)
        {
            NetworkingManager.AssignTeam(SNet.LocalPlayer, (int) realTeam);
            Plugin.L.LogDebug($"Switching to team {team}. ({realTeam})");
            return $"Switching to team {team}. ({SimplifyTeam(realTeam)})";
        }
        
        return $"Already on team {team}.";
    }
    
    private static string ToggleSpectatorMode(string[] arg)
    {
        if (SpectatorController.IsActive)
        {
            SpectatorController.TryExit();
            return "Exiting spectator mode.";
        }
        
        if (SpectatorController.TryEnter())
            return "Entering spectator mode.";
        return "Couldn't enter spectator mode.";
    }

    private static string SelectMelee(string[] arg)
    {
        _gearMeleeSelector.Show();
        
        return string.Empty;
    }

    private static string SelectTool(string[] args)
    {

        if (NetSessionManager.HasSession)
        {
            if (!IsLocalPlayerAllowedToPickTool)
                return $"<i><color=red>Not allowed to change tool.</color></i> (CD: {(_pickToolCooldownEnd - DateTimeOffset.UtcNow).Seconds} seconds)";
        }
        
        var info = NetworkingManager.GetLocalPlayerInfo();

        if (IsSeeker(info.Team))
        {
            _gearSeekerSelector.Show();
            return string.Empty;
        }
        
        _gearHiderSelector.Show();
        return string.Empty;
    }

    private static string Disinfect(string[] args)
    {
        if (NetSessionManager.HasSession)
            return "Can't use this command while a round is active!";
        
        Utils.SetLocalPlayerInfection(0f);
        
        return string.Empty;
    }

    private static string JumpDimension(string[] args)
    {
        if (!SNet.IsMaster)
            return "Master only.";
        
        if (NetSessionManager.HasSession)
            return "Can't use this command while a round is active!";

        var dimension = eDimensionIndex.Reality;
        if (args.Length >= 1)
        {
            var dimArg = args[0];

            if (!int.TryParse(dimArg, out var dimInt))
                return "Couldn't parse dimension.";
            
            dimension = (eDimensionIndex) dimInt;
        }
        
        if (dimension < eDimensionIndex.Reality || dimension >= eDimensionIndex.Dimension_17)
            return "Invalid dimension.";
        
        var localPlayer = PlayerManager.GetLocalPlayerAgent();

        var allDims = LG_LevelBuilder.Current.m_currentFloor.m_dimensions;

        var dim = allDims.ToArray().FirstOrDefault(dim => dim.DimensionIndex == dimension);

        if (dim == null)
            return "Dimension wasn't found. :c";
        
        var targetPos = dim.GetStartCourseNode().GetRandomPositionInside();
        
        foreach (var player in NetworkingManager.AllValidPlayers)
        {
            if (!player.HasAgent)
                continue;

            if (player.PlayerAgent.DimensionIndex == dimension)
                continue;

            NetworkingManager.SendForceTeleport(player, targetPos, localPlayer.FPSCamera.Forward, dimension, PlayerAgent.WarpOptions.PlaySounds | PlayerAgent.WarpOptions.ShowScreenEffectForLocal | PlayerAgent.WarpOptions.WithoutBots);
        }
        
        return string.Empty;
    }

    private static string Unstuck(string[] args)
    {
        if (args.Length < 1 || args[0] != "confirm")
        {
            Gamemodes.Plugin.PostLocalMessage("<color=orange>This command will teleport you to your last GoodPos, ...</color>");
            return "<color=red><b>but also down you, confirm using </color><color=orange>/unstuck confirm</color></b>";
        }

        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return "No local player.";

        if (localPlayer.TryWarpTo(localPlayer.DimensionIndex, localPlayer.GoodPosition, localPlayer.FPSCamera.Forward,
                PlayerAgent.WarpOptions.ShowScreenEffectForLocal | PlayerAgent.WarpOptions.PlaySounds |
                PlayerAgent.WarpOptions.WithoutBots))
        {
            localPlayer.Locomotion.DisableFallDamageTemporarily = false;
            localPlayer.Damage.FallDamage(999f);
        }

        return "Tried to unstuck.";
    }

    private static string PrintTimes(string[] arg)
    {
        _timeKeeper.PrintTotalTime();
        return string.Empty;
    }

    private static string FogTest(string[] arg)
    {
        if (!SNet.IsMaster)
            return "Nope";

        PlayerManager.TryGetLocalPlayerAgent(out var localPlayer);

        if (CreateAndPlayFogSphere(localPlayer.Position))
        {
            return "fogSphere.Play() failed";
        }

        return "testing";
    }

    private static bool CreateAndPlayFogSphere(Vector3 position)
    {
        var go = new GameObject("FogSphere test");

        go.transform.position = position;

        var fogSphere = go.AddComponent<FogSphereHandler>();

        fogSphere.m_range = 20f;
        fogSphere.m_rangeMax = 20f;
        fogSphere.m_rangeMin = 0f;

        fogSphere.m_totalLength = 15f;

        fogSphere.m_intensityMin = 0f;
        
        fogSphere.m_density = 2f;
        fogSphere.m_densityMax = 2f;
        fogSphere.m_densityMin = 0f;

        fogSphere.m_densityAmountMax = 1f;
        fogSphere.m_densityAmountMin = 1f;

        fogSphere.m_rangeCurve.AddKey(0f, 0f);
        fogSphere.m_rangeCurve.AddKey(0.08f, 20f);
        fogSphere.m_rangeCurve.AddKey(0.5f, 20f);
        fogSphere.m_rangeCurve.AddKey(1f, 12.5f);

        fogSphere.m_densityCurve.AddKey(0f, 0f);
        fogSphere.m_densityCurve.AddKey(0.01f, 2f);
        fogSphere.m_densityCurve.AddKey(0.05f, 2f);
        fogSphere.m_densityCurve.AddKey(0.5f, 1f);
        fogSphere.m_densityCurve.AddKey(0.9f, 0.1f);
        fogSphere.m_densityCurve.AddKey(1f, 0f);
        
        if (!fogSphere.Play())
        {
            UnityEngine.Object.DestroyImmediate(go);
            return true;
        }

        return false;
    }
}