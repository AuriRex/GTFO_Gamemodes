using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Gamemodes.Core;

public static class ToolInstanceCaches
{
    public static readonly ToolInstanceCache<MineDeployerInstance> MineCache = new();
    public static readonly ToolInstanceCache<GlueGunProjectile> GlueCache = new();
    public static readonly ToolInstanceCache<SentryGunInstance> SentryCache = new();

    internal static void ResetAll()
    {
        MineCache.Clear();
        GlueCache.Clear();
        SentryCache.Clear();
    }
}

public class ToolInstanceCache<T> where T : MonoBehaviour
{
    public IEnumerable<T> All => _instances.Where(i => i != null).ToArray();
    private readonly List<T> _instances = new();

    public void Register(T item)
    {
        if (_instances.Any(i => i?.Pointer == item.Pointer))
            return;
        
        _instances.Add(item);
    }

    internal void Clear()
    {
        _instances.Clear();
    }
    
    public void Deregister(T item)
    {
        var instance = _instances.FirstOrDefault(i => i?.Pointer == item.Pointer);

        if (instance == null)
            return;
        
        _instances.Remove(instance);
    }
}