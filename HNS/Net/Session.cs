using System;

namespace HNS.Net;

internal class Session
{
    public DateTimeOffset StartTime { get; private set; }
    public DateTimeOffset EndTime { get; private set; }
    public bool IsActive { get; internal set; }

    public Session()
    {
        StartTime = DateTimeOffset.UtcNow;
        IsActive = true;
    }


    internal void EndSession()
    {
        EndTime = DateTimeOffset.UtcNow;
        IsActive = false;
    }
}
