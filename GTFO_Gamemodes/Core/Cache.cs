using System.Collections.Generic;

namespace Gamemodes.Core;

public class Cache<T>
{
    private readonly Dictionary<string, T> _data = new();

    public bool TryGetCached(string id, out T data)
    {
        return _data.TryGetValue(id, out data);
    }

    public void DoCache(string id, T data)
    {
        _data.TryAdd(id, data);
    }
}