using System;
using Gamemodes.Net;
using SNetwork;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Gamemodes.Core;

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
    
    public static bool LocalPlayerHideIcons()
    {
        return GamemodeManager.CurrentMode.TeamVisibility.LocalPlayerHideIconsInstance();
    }

    public bool LocalPlayerHideIconsInstance()
    {
        var localInfo = NetworkingManager.GetLocalPlayerInfo();

        if (!_meta.TryGetValue(localInfo.Team, out var metadata))
            return false;

        return metadata.hideLocalPlayerIcons;
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
    private readonly Dictionary<int, TeamMetadata> _meta = new();

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

    public TeamVisContract Team<T>(T team) where T : struct, IConvertible
    {
        return Team(team.ToInt32(CultureInfo.InvariantCulture));
    }

    public bool CanLocalPlayerSee(int otherTeam)
    {
        var localInfo = NetworkingManager.GetLocalPlayerInfo();

        return CanTeamSee(localInfo.Team, otherTeam);
    }

    public bool CanLocalPlayerSee<T>(T otherTeam) where T : struct, IConvertible
    {
        return CanLocalPlayerSee(otherTeam.ToInt32(CultureInfo.InvariantCulture));
    }
    
    public bool CanTeamSee(int team, int otherTeam)
    {
        if (!_visibility.TryGetValue(team, out var set))
            return false;

        return set.Contains(otherTeam);
    }

    public bool CanTeamSee<T>(T team, T otherTeam) where T : struct, IConvertible
    {
        return CanTeamSee(team.ToInt32(CultureInfo.InvariantCulture), otherTeam.ToInt32(CultureInfo.InvariantCulture));
    }

    private TeamMetadata GetMeta(int team)
    {
        if (_meta.TryGetValue(team, out var metadata))
        {
            return metadata;
        }

        metadata = new();
        _meta.Add(team, metadata);

        return metadata;
    }
    
    public void MetaSetLocalPlayerHidden(int team, bool value)
    {
        var metadata = GetMeta(team);

        metadata.hideLocalPlayerIcons = true;
    }
}

public class TeamMetadata
{
    public bool hideLocalPlayerIcons = false;
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

    public TeamVisContract WithLocalPlayerIconsHidden()
    {
        vis.MetaSetLocalPlayerHidden(_team, true);
        return this;
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

    public TeamVisContractExtended CanSee<T>(T y) where T : struct, IConvertible
    {
        return CanSee(y.ToInt32(CultureInfo.InvariantCulture));
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

    public TeamVisContractExtended CanSee<T>(params T[] y) where T : struct, IConvertible
    {
        return CanSee(y.Select(x => x.ToInt32(CultureInfo.InvariantCulture)).ToArray());
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

    public TeamVisibility And<T>(params T[] y) where T : struct, IConvertible
    {
        return And(y.Select(x => x.ToInt32(CultureInfo.InvariantCulture)).ToArray());
    }

    public TeamVisContract Team(int team)
    {
        return new TeamVisContract(_contract.vis, team);
    }

    public TeamVisContract Team<T>(T team) where T : struct, IConvertible
    {
        return Team(team.ToInt32(CultureInfo.InvariantCulture));
    }
}
