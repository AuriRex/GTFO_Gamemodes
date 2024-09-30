using Gamemodes.Net;
using System.Collections.Generic;

namespace Gamemodes.Mode;

public class TeamVisibility
{
    public Dictionary<int, HashSet<int>> _visibility = new();

    internal void Reset()
    {
        _visibility = new();
    }

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

    public bool LocalPlayerCanSee(int otherTeam)
    {
        var localInfo = NetworkingManager.GetLocalPlayerInfo();

        return CanSee(localInfo.Team, otherTeam);
    }

    public bool CanSee(int team, int otherTeam)
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

    public TeamVisibility CanSee(int y)
    {
        return CanSee(new int[] { y });
    }

    public TeamVisibility CanSee(params int[] y)
    {
        vis.Get(_team, out var set);

        foreach (var entry in y)
        {
            set.Add(entry);
        }

        return vis;
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
