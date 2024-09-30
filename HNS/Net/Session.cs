using System;

namespace HNS.Net;

internal class Session
{
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public bool IsActive { get; internal set; }
    public TimeSpan FinalTime { get; private set; }

    public Session()
    {
        StartTime = DateTimeOffset.UtcNow;
        IsActive = true;
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
