using System.Linq;
using Gamemodes.Net;
using HNS.Net;
using SNetwork;

namespace HNS.Core;

internal partial class HideAndSeekMode
{
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
}