using Gamemodes.Net;
using SNetwork;
using System.Collections.Generic;

namespace Gamemodes.Mode;

public class TeamVisibility
{
    public static bool LocalPlayerCanSee(SNet_Player player)
    {
        NetworkingManager.GetPlayerInfo(player, out var other);

        return LocalPlayerCanSeeTeam(other.Team);
    }

    public static bool LocalPlayerCanSeeTeam(int team)
    {
        return GamemodeManager.CurrentMode.TeamVisibility.CanLocalPlayerSee(team);
    }

    public static bool PlayerCanSee(SNet_Player player, SNet_Player other)
    {
        NetworkingManager.GetPlayerInfo(player, out var playerInfo);
        NetworkingManager.GetPlayerInfo(other, out var otherInfo);

        return PlayerCanSee(playerInfo.Team, otherInfo.Team);
    }

    public static bool PlayerCanSee(int team, int teamOther)
    {
        return GamemodeManager.CurrentMode.TeamVisibility.CanTeamSee(team, teamOther);
    }

    private readonly Dictionary<int, HashSet<int>> _visibility = new();

    internal void Get(int team, out HashSet<int> set)
    {
        if (!_visibility.TryGetValue(team, out set))
        {
            set = new();
            _visibility[team] = set;
        }
    }

    public TeamVisContract Team(int team)
    {
        return new TeamVisContract(this, team);
    }

    public bool CanLocalPlayerSee(int otherTeam)
    {
        var localInfo = NetworkingManager.GetLocalPlayerInfo();

        return CanTeamSee(localInfo.Team, otherTeam);
    }

    public bool CanTeamSee(int team, int otherTeam)
    {
        if (!_visibility.TryGetValue(team, out var set))
            return false;

        return set.Contains(otherTeam);
    }
}

public class TeamVisContract
{
    internal readonly TeamVisibility vis;
    private readonly int _team;
    internal TeamVisContract(TeamVisibility tvis, int x)
    {
        vis = tvis;
        _team = x;
    }

    public TeamVisContractExtended CanSeeSelf()
    {
        CanSee(_team);
        return new TeamVisContractExtended(this);
    }

    public TeamVisContractExtended CanSee(int y)
    {
        CanSee(new int[] { y });
        return new TeamVisContractExtended(this);
    }

    public TeamVisContractExtended CanSee(params int[] y)
    {
        vis.Get(_team, out var set);

        foreach (var entry in y)
        {
            set.Add(entry);
        }

        return new TeamVisContractExtended(this);
    }
}

public class TeamVisContractExtended
{
    private readonly TeamVisContract _contract;

    internal TeamVisContractExtended(TeamVisContract contract)
    {
        _contract = contract;
    }

    public TeamVisibility And(params int[] y)
    {
        _contract.CanSee(y);
        return _contract.vis;
    }

    public TeamVisContract Team(int team)
    {
        return new TeamVisContract(_contract.vis, team);
    }
}
