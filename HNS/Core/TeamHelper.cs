namespace HNS.Core;

internal static class TeamHelper
{
    public static GMTeam GetSeekerTeamForHiders(GMTeam hiderTeam)
    {
        return hiderTeam switch
        {
            GMTeam.Hiders => GMTeam.Seekers,
            GMTeam.HiderAlpha => GMTeam.SeekerAlpha,
            GMTeam.HiderBeta => GMTeam.SeekerBeta,
            GMTeam.HiderGamma => GMTeam.SeekerGamma,
            GMTeam.HiderDelta => GMTeam.SeekerDelta,
            _ => hiderTeam,
        };
    }

    public static GMTeam GetHiderTeamForSeekers(GMTeam seekerTeam)
    {
        return seekerTeam switch
        {
            GMTeam.Seekers => GMTeam.Hiders,
            GMTeam.SeekerAlpha => GMTeam.HiderAlpha,
            GMTeam.SeekerBeta => GMTeam.HiderBeta,
            GMTeam.SeekerGamma => GMTeam.HiderGamma,
            GMTeam.SeekerDelta => GMTeam.HiderDelta,
            _ => seekerTeam,
        };
    }

    public static GMTeam GetPreGameTeamForPlayer(GMTeam playerTeam)
    {
        playerTeam = GetSeekerTeamForHiders(playerTeam);

        return playerTeam switch
        {
            GMTeam.Seekers => GMTeam.PreGame,
            GMTeam.SeekerAlpha => GMTeam.PreGameAlpha,
            GMTeam.SeekerBeta => GMTeam.PreGameBeta,
            GMTeam.SeekerGamma => GMTeam.PreGameGamma,
            GMTeam.SeekerDelta => GMTeam.PreGameDelta,
            _ => playerTeam,
        };
    }

    public static GMTeam GetHiderTeamForPlayer(GMTeam playerTeam)
    {
        playerTeam = GetPreGameTeamForPlayer(playerTeam);

        return playerTeam switch
        {
            GMTeam.PreGame => GMTeam.Hiders,
            GMTeam.PreGameAlpha => GMTeam.HiderAlpha,
            GMTeam.PreGameBeta => GMTeam.HiderBeta,
            GMTeam.PreGameGamma => GMTeam.HiderGamma,
            GMTeam.PreGameDelta => GMTeam.HiderDelta,
            _ => playerTeam,
        };
    }

    public static GMTeam GetSeekerTeamForPlayer(GMTeam playerTeam)
    {
        playerTeam = GetHiderTeamForPlayer(playerTeam);
        return GetSeekerTeamForHiders(playerTeam);
    }

    public static GMTeam LocalGetPreGameTeam(GMTeam team)
    {
        return HideAndSeekMode._localTeam switch
        {
            HNSTeam.Alpha => GMTeam.PreGameAlpha,
            HNSTeam.Beta => GMTeam.PreGameBeta,
            HNSTeam.Gamma => GMTeam.PreGameGamma,
            HNSTeam.Delta => GMTeam.PreGameDelta,
            _ => team
        };
    }

    public static GMTeam LocalGetRealTeam(GMTeam team)
    {
        switch (team)
        {
            case GMTeam.Camera:
                return team;
            case GMTeam.PreGame:
            case GMTeam.PreGameAlpha:
            case GMTeam.PreGameBeta:
            case GMTeam.PreGameGamma:
            case GMTeam.PreGameDelta:
                return LocalGetPreGameTeam(team);
            default:
                if (HideAndSeekMode._localTeam != HNSTeam.None)
                    break;
                team = IsHider(team) ? GMTeam.Hiders : GMTeam.Seekers;
                break;
        }

        return HideAndSeekMode._localTeam switch
        {
            HNSTeam.Alpha => IsHider(team) ? GMTeam.HiderAlpha : GMTeam.SeekerAlpha,
            HNSTeam.Beta => IsHider(team) ? GMTeam.HiderBeta : GMTeam.SeekerBeta,
            HNSTeam.Gamma => IsHider(team) ? GMTeam.HiderGamma : GMTeam.SeekerGamma,
            HNSTeam.Delta => IsHider(team) ? GMTeam.HiderDelta : GMTeam.SeekerDelta,
            _ => team
        };
    }

    public static bool IsHider(int team) => IsHider((GMTeam)team);

    public static bool IsHider(GMTeam team)
    {
        switch (team)
        {
            case GMTeam.Hiders:
            case GMTeam.HiderAlpha:
            case GMTeam.HiderBeta:
            case GMTeam.HiderGamma:
            case GMTeam.HiderDelta:
                return true;
            default:
                return false;
        }
    }

    public static bool IsSeeker(int team) => IsSeeker((GMTeam)team);

    public static bool IsSeeker(GMTeam team)
    {
        switch (team)
        {
            case GMTeam.Seekers:
            case GMTeam.SeekerAlpha:
            case GMTeam.SeekerBeta:
            case GMTeam.SeekerGamma:
            case GMTeam.SeekerDelta:
                return true;
            default:
                return false;
        }
    }

    public static GMTeam SimplifyTeam(GMTeam team)
    {
        if (IsSeeker(team))
            return GMTeam.Seekers;
        if (IsHider(team))
            return GMTeam.Hiders;

        return team switch
        {
            GMTeam.Camera => GMTeam.Camera,
            _ => GMTeam.PreGame,
        };
    }
}