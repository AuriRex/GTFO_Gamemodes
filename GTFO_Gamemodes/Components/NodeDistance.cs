using System;
using System.Collections.Generic;
using System.Linq;
using AIGraph;
using Player;
using UnityEngine;

namespace Gamemodes.Components;

public class NodeDistance : MonoBehaviour
{
    public static NodeDistance Instance { get; private set; }

    public const int DEFAULT_MAX_NODE_DISTANCE = 3;
    public int MaxNodeDistanceToCheck = DEFAULT_MAX_NODE_DISTANCE;
    
    private const float UPDATE_INTERVAL = 1f;
    private LocalPlayerAgent _localPLayer;
    private float _nextUpdate;
    
    private static PlayerAgent[] _agentsInLevel;
    private static readonly Dictionary<ulong, int> _distances = new();
    private static readonly HashSet<IntPtr> _visitedAreas = new();

    public void Reset()
    {
        MaxNodeDistanceToCheck = DEFAULT_MAX_NODE_DISTANCE;
    }
    
    public void Start()
    {
        _localPLayer = GetComponent<LocalPlayerAgent>();
        
        if (_localPLayer == null || Instance != null)
            Destroy(this);

        Instance = this;
    }

    public void Update()
    {
        var time = Time.realtimeSinceStartup;

        if (time >= _nextUpdate)
        {
            _nextUpdate = time + UPDATE_INTERVAL;

            DoUpdate();
        }
    }

    private void DoUpdate()
    {
        if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            return;
        
        var localNode = _localPLayer.CourseNode;

        _agentsInLevel = PlayerManager.PlayerAgentsInLevel.ToArray()
            .Where(p => p.DimensionIndex == _localPLayer.DimensionIndex
            && !p.IsLocallyOwned).ToArray();
        
        _visitedAreas.Clear();
        _distances.Clear();

        PropagateAreaCheck(localNode, maxDistance: MaxNodeDistanceToCheck);
    }

    public static void GetDistance(ulong playerId, out int distance)
    {
        distance = _distances.GetValueOrDefault(playerId, 100);
    }
    
    private static void PropagateAreaCheck(AIG_CourseNode node, int maxDistance, int currentDistance = 0)
    {
        if (!CheckArea(node, currentDistance))
            return;

        if (maxDistance <= 0)
            return;
        
        foreach (var portal in node.m_portals)
        {
            // Closed doors
            if (!portal.IsTraversable)
                continue;
            
            if (node.m_area == portal.m_nodeA.m_area)
            {
                PropagateAreaCheck(portal.m_nodeB, maxDistance - 1, currentDistance + 1);
                continue;
            }
            
            PropagateAreaCheck(portal.m_nodeA, maxDistance - 1, currentDistance + 1);
        }
    }
    
    private static bool CheckArea(AIG_CourseNode node, int distance)
    {
        if (!_visitedAreas.Add(node.m_area.Pointer))
            return false;

        foreach (var player in _agentsInLevel)
        {
            if (player.CourseNode.m_area == node.m_area)
            {
                _distances[player.Owner.Lookup] = distance;
            }
        }

        return true;
    }
}