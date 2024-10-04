using System;

namespace HNS.Net;

public class Session
{
    public DateTimeOffset StartTime { get; private set; }
    public int SetupDuration { get; private set; } = 60;
    public DateTimeOffset SetupTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public bool IsActive { get; internal set; }
    public TimeSpan HidingTime { get; private set; } = TimeSpan.Zero;
    public TimeSpan FinalTime { get; private set; }
    public bool SetupTimeFinished => SetupTime <= DateTimeOffset.UtcNow;

    public Session(int setupDuration = 60)
    {
        StartTime = DateTimeOffset.UtcNow;
        SetupDuration = setupDuration;
        SetupTime = StartTime.AddSeconds(setupDuration);
        IsActive = true;
    }

    internal void LocalPlayerCaught()
    {
        HidingTime = DateTimeOffset.UtcNow - SetupTime;

        if (HidingTime.TotalSeconds < 0)
        {
            HidingTime = TimeSpan.Zero;
        }
    }

    internal void EndSession(DateTimeOffset? endTime = null)
    {
        if (!IsActive)
            return;

        EndTime = endTime ?? DateTimeOffset.UtcNow;
        FinalTime = EndTime - StartTime;
        IsActive = false;
    }
}
