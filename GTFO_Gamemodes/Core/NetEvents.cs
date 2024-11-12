using System;
using System.Collections.Generic;
using Gamemodes.Net;
using SNetwork;

namespace Gamemodes.Core;

public class NetEvents
{
    private readonly Dictionary<Type, Delegate> _eventStorage = new();

    public void SendEvent<T>(T eventData, SNet_Player targetPlayer = null, bool invokeLocal = false) where T : struct
    {
        NetworkingManager.SendEvent(eventData, targetPlayer, invokeLocal);
    }
    
    public void RegisterEvent<T>(Action<ulong, T> onReceive) where T : struct
    {
        _eventStorage.Add(typeof(T), onReceive);

        NetworkingManager.RegisterEventWrapped<T>();
    }

    public bool HasEvent<T>() where T : struct
    {
        return _eventStorage.ContainsKey(typeof(T));
    }

    public bool InvokeEvent<T>(ulong sender, T data) where T : struct
    {
        if (!_eventStorage.TryGetValue(typeof(T), out var handler))
        {
            return false;
        }

        var action = (Action<ulong, T>)handler;

        action.Invoke(sender, data);
        return true;
    }
}