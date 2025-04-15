using System;
using System.Collections.Generic;
using HNS.Net;

namespace HNS.Core;

public class TimeKeeper
{
    private readonly List<Session> _sessions = new();
    
    public void PushSession(Session session)
    {
        _sessions.Add(session);
    }

    public void PrintTotalTime()
    {
        if (_sessions.Count == 0)
        {
            Gamemodes.Plugin.PostLocalMessage("<#FCC>No times recorded yet.</color>");
            return;
        }
        
        Gamemodes.Plugin.PostLocalMessage("<#FFF>Time spent hiding:</color>");
        Plugin.L.LogWarning($"{nameof(TimeKeeper)}: Time spent hiding:");

        TimeSpan totalTime = TimeSpan.Zero;
        
        var count = 1;
        foreach (var session in _sessions)
        {
            var wasSeeker = session.HidingTime <= TimeSpan.Zero;

            var col = count % 2 == 0 ? "<#DDD>" : "<#999>";
            var timeOrSeeker = wasSeeker ? "<#FCC>Seeker</color>" : session.HidingTime.ToString(@"mm\:ss");
            
            Gamemodes.Plugin.PostLocalMessage($"{col}#{count}: {timeOrSeeker}</color>");
            Plugin.L.LogInfo($"#{count}: {(wasSeeker ? "Seeker" : session.HidingTime.ToString(@"mm\:ss"))}");
            
            totalTime += session.HidingTime;
            
            count++;
        }
        
        Gamemodes.Plugin.PostLocalMessage($"<color=orange>Total hiding time: <#CFC>{totalTime:hh\\:mm\\:ss}</color></color>");
        Plugin.L.LogWarning($"{nameof(TimeKeeper)}: Total hiding time: {totalTime:hh\\:mm\\:ss}");
    }

    public void ClearSessions()
    {
        Plugin.L.LogInfo($"{nameof(TimeKeeper)} session data cleared.");
        _sessions.Clear();
    }
}