using System;
using System.Collections.Generic;
using System.Linq;
using AIGraph;
using Gamemodes.Core;
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
        
        var settings = GamemodeManager.CurrentSettings;
        if (settings == null)
            return;

        if (!settings.UseNodeDistance && !settings.UseProximityVoiceChat)
            return;

        var localNode = _localPLayer.CourseNode;

        _agentsInLevel = PlayerManager.PlayerAgentsInLevel.ToArray()
            .Where(p => p.DimensionIndex == _localPLayer.DimensionIndex
            && !p.IsLocallyOwned).ToArray();
        
        _visitedAreas.Clear();
        _distances.Clear();

        //Plugin.L.LogInfo("--- start ---");
        PopulateNodeDistance(localNode, maxDistance: MaxNodeDistanceToCheck);
        //Plugin.L.LogInfo("---  end  ---");
    }

    public static void GetDistance(ulong playerId, out int distance)
    {
        distance = _distances.GetValueOrDefault(playerId, 100);
    }

    private static readonly HashSet<IntPtr> _visitedTwo = new();
    private static readonly Queue<AIG_CourseNode> _queue = new();
    
    private static void PopulateNodeDistance(AIG_CourseNode node, int maxDistance)
    {
        _visitedTwo.Clear();
        _queue.Clear();

        var depth = CheckAreaAndQueue(node, _queue, _visitedTwo, 0);
        var count = 0;
        var acc = 0;

        var currentDistance = 1;
        
        while (_queue.Count > 0)
        {
            var childNode = _queue.Dequeue();

            if (count >= depth)
            {
                count = 0;
                depth = acc;
                acc = 0;
                currentDistance++;
                if (currentDistance > maxDistance)
                    break;
            }
            
            count++;
            
            acc += CheckAreaAndQueue(childNode, _queue, _visitedTwo, currentDistance);
        }
    }

    private static int CheckAreaAndQueue(AIG_CourseNode node, Queue<AIG_CourseNode> queue, HashSet<IntPtr> visited, int currentDistance)
    {
        if (!CheckArea(node, currentDistance))
            return 0;

        var countPre = queue.Count;
        
        foreach (var portal in node.m_portals)
        {
            // Closed doors
            if (!portal.IsTraversable)
                continue;

            var otherNode = node.m_area == portal.m_nodeA.m_area ? portal.m_nodeB : portal.m_nodeA;

            if (!visited.Add(otherNode.m_area.Pointer))
                continue;
            
            queue.Enqueue(otherNode);
        }
        
        return queue.Count - countPre;
    }
    
    private static bool CheckArea(AIG_CourseNode node, int distance)
    {
        if (!_visitedAreas.Add(node.m_area.Pointer))
            return false;

        //Plugin.L.LogDebug($"Checking area: {(DebugGetDashes(distance))} {node.m_area.m_zone.NavInfo.Number}_{node.m_area.m_navInfo.Suffix}");
        
        foreach (var player in _agentsInLevel)
        {
            if (player.CourseNode.m_area == node.m_area)
            {
                _distances[player.Owner.Lookup] = distance;
                
                //Plugin.L.LogDebug($"Player {player.Owner.NickName}: distance: {distance}");
            }
        }

        return true;
    }

    private static string DebugGetDashes(int distance)
    {
        string str = string.Empty;
        for (int i = 0; i < distance; i++)
        {
            str += "-";
        }
        return str;
    }
}