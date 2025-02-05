using System;
using System.Linq;
using Gamemodes.Core;
using Gamemodes.Net;
using HNS.Net;
using SNetwork;

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

        var seekers = NetworkingManager.AllValidPlayers.Where(pw => pw.Team == (int)GMTeam.Seekers).Select(pw => pw.ID)
            .ToArray();

        NetSessionManager.SendStartGamePacket(seekers);

        return string.Empty;
    }

    private static string StopGame(string[] args)
    {
        if (!SNet.IsMaster)
            return "Only the Master can stop.";

        NetSessionManager.SendStopGamePacket();

        return string.Empty;
    }

    private static string SwitchToLobby(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.PreGameAndOrSpectator);
        return string.Empty;
    }

    private static string SwitchToHider(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.Hiders);
        return string.Empty;
    }

    private static string SwitchToSeeker(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.Seekers);
        return string.Empty;
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

        if (info.Team == (int)GMTeam.Seekers)
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
}