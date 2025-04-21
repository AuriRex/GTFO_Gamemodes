using System.Runtime.CompilerServices;
using Agents;
using Enemies;
using Gamemodes.Net;
using HarmonyLib;
using HNS.Core;
using Player;

namespace HNS.Patches;

[HarmonyPatch(typeof(AgentAI), nameof(AgentAI.IsTargetValid), MethodType.Getter)]
public static class AgentAI__IsTargetValid__Patch
{
    public static bool Prefix(AgentAI __instance, ref bool __result)
    {
        //Plugin.L.LogDebug($"AgentAI.IsTargetValid called for {__instance.m_target.m_agent.name}");
        var playerAgent = __instance.m_target?.m_agent?.TryCast<PlayerAgent>();

        if (playerAgent == null)
            return true;

        NetworkingManager.GetPlayerInfo(playerAgent.Owner, out var info);
        
        if (TeamHelper.IsHider(info.Team) && playerAgent.Alive)
        {
            //Plugin.L.LogDebug($"Valid Target: {__instance.m_target.m_agent.name}");
            __result = true;
            return false;
        }
        
        __result = false;
        return false;
    }
}

[HarmonyPatch(typeof(EnemyDetection), nameof(EnemyDetection.UpdateTargets))]
public static class EnemyTargetingPatch
{
    public static bool Prefix(EnemyDetection __instance)
    {
        //Plugin.L.LogDebug("UpdateTargets called");
        UpdateTargets(__instance);
        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void UpdateTargets(EnemyDetection _this)
    {
        _this.m_ai.m_behaviourData.Targets.Clear();
        
        foreach (var player in NetworkingManager.AllValidPlayers)
        {
            if (player == null || !player.HasAgent)
                continue;

            if (!TeamHelper.IsHider(player.Team))
                continue;
            
            var target = _this.m_ai.m_behaviourData.GetTarget(player.PlayerAgent);
            _this.m_ai.m_behaviourData.Targets.Add(target);
        }

        _this.m_movementDetectionDistance = _this.m_ai.m_enemyAgent.EnemyDetectionData.movementDetectionDistance;
        _this.m_detectionBuildupSpeed = _this.m_ai.m_enemyAgent.EnemyDetectionData.detectionBuildupSpeed;
        _this.m_detectionCooldownSpeed = _this.m_ai.m_enemyAgent.EnemyDetectionData.detectionCooldownSpeed;
    }
}