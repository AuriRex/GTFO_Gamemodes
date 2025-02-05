using System;
using System.Collections.Generic;
using Gamemodes.Net;
using Gamemodes.Net.Packets;
using UnityEngine;

namespace ExtraModes.Net;

public class RayManager : IDisposable
{
    private readonly List<Collider> _hitColliders = new();
    private readonly List<pRayCast> _executedRayCastCommands = new();
    
    public RayManager()
    {
        NetworkingManager.OnRayCastInstructionsReceived += ProcessRayCastCommand;
    }

    public void Dispose()
    {
        NetworkingManager.OnRayCastInstructionsReceived -= ProcessRayCastCommand;

        Reset();
    }

    public void MasterHandleLateJoiner(PlayerWrapper lateJoiner)
    {
        foreach (var cmd in _executedRayCastCommands)
        {
            NetworkingManager.SendEvent(cmd, lateJoiner, invokeLocal: false);
        }
    }
    
    private void ProcessRayCastCommand(pRayCast data)
    {
        if (data.Type == 1)
        {
            Reset();
            return;
        }
        
        _executedRayCastCommands.Add(data);
        
        if (!Physics.Raycast(data.Origin, data.Direction, out var hit, maxDistance: 50f, layerMask: LayerManager.MASK_WORLD))
            return;

        var collider = hit.collider;
        
        _hitColliders.Add(collider);
        
        collider.enabled = false;
    }

    public void Reset()
    {
        foreach (var col in _hitColliders)
        {
            if (col == null)
                continue;
            col.enabled = true;
        }
        
        _hitColliders.Clear();
    }
}