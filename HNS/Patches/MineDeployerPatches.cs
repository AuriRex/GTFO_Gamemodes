using System;
using System.Collections;
using System.Linq;
using System.Runtime.CompilerServices;
using Agents;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using FX_EffectSystem;
using Gamemodes.Extensions;
using Gamemodes.Net;
using HarmonyLib;
using HNS.Components;
using HNS.Core;
using HNS.Extensions;
using Player;
using SNetwork;
using UnityEngine;

namespace HNS.Patches;

[HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.Setup))]
public static class MineDeployerPatches
{
    private static readonly Color COLOR_BARELY_VISIBLE = new Color(0.0118f, 0, 0, 1);
    public static void Postfix(MineDeployerInstance __instance)
    {
        __instance.Mode = eStickyMineMode.Alarm;

        __instance.gameObject.GetOrAddComponent<CustomMineController>();
    }
}


[HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.FixedUpdate))]
public static class MineDeployerInstance_FixedUpdate_Patch
{
    // Allow clients to run mine detection code
    // So we can manage detection of the local player client side
    // to avoid network jank for whenever the mine detection is on or off
    public static bool Prefix(MineDeployerInstance __instance)
    {
        if (!__instance.Alive || !__instance.m_detectionEnabled)
        {
            return false;
        }

        __instance.m_detection.UpdateDetection();

        if (!SNet.IsMaster)
        {
            return false;
        }

        if (__instance.m_detection.DetectionRange == __instance.m_lastDetectionRange)
        {
            return false;
        }

        __instance.m_lastDetectionRange = __instance.m_detection.DetectionRange;
        MineDeployerInstance.pTripLineUpdate data = new MineDeployerInstance.pTripLineUpdate
        {
            lineLength = __instance.m_detection.DetectionRange
        };
        __instance.m_initPacket.Send(data, SNet_ChannelType.GameNonCritical);

        return false;
    }
}

[HarmonyPatch(typeof(MineDeployerInstance_Detect_Laser), nameof(MineDeployerInstance_Detect_Laser.Setup))]
public static class MineDeployerDetection_Patch
{
    public static void Postfix(MineDeployerInstance_Detect_Laser __instance)
    {
        __instance.m_scanMask = SCAN_MASK;
        __instance.m_enemyMask = ENEMY_MASK;
    }
    
    private static int _SCAN_MASK;
    public static int SCAN_MASK
    {
        get
        {
            if (_SCAN_MASK == 0)
            {
                _SCAN_MASK = LayerMask.GetMask(new[]
                {
                    "EnemyDamagable",
                    "Default",
                    "Default_NoGraph",
                    "Default_BlockGraph",
                    "Dynamic",
                    "PlayerSynced",
                    "PlayerMover"
                });
            }

            return _SCAN_MASK;
        }
    }

    private static int _ENEMY_MASK;
    public static int ENEMY_MASK
    {
        get
        {
            if (_ENEMY_MASK == 0)
            {
                _ENEMY_MASK = LayerMask.GetMask(new[]
                {
                    "EnemyDamagable",
                    "PlayerSynced",
                    "PlayerMover"
                });
            }

            return _ENEMY_MASK;
        }
    }

}

[HarmonyPatch(typeof(MineDeployerInstance_Detect_Laser), nameof(MineDeployerInstance_Detect_Laser.UpdateDetection))]
public static class MineDeployerInstance_UpdateDetection_Patch
{
    public static event Action<MineDeployerInstance, Agent> OnAgentDetected;
    
    public static bool Prefix(MineDeployerInstance_Detect_Laser __instance)
    {
        CustomUpdateDetection(__instance);

        return false;
    }
    
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static void CustomUpdateDetection(MineDeployerInstance_Detect_Laser _this)
    {
        if (_this.m_core.Mode != eStickyMineMode.Explode && _this.m_core.Mode != eStickyMineMode.Alarm)
        {
            return;
        }

        float num = _this.DetectionRange;
        if (_this.m_maxLineDistance > 0f)
        {
            if (Physics.SphereCast(_this.m_lineRendererAlign.position, 0.1f, _this.m_lineRendererAlign.forward, out RaycastHit raycastHit, _this.m_maxLineDistance, _this.m_scanMask))
            {
                num = raycastHit.distance;
                var hitGo = raycastHit.collider.gameObject;
                if (_this.m_enemyMask.IsInLayerMask(hitGo))
                {
                    var agent = hitGo.GetComponentInParents<Agent>();
                    if (agent == null)
                    {
                        agent = hitGo.GetComponent<LocalPlayerAgent>();
                    }
                    //Plugin.L.LogWarning($"Mine detected agent: {agent?.name}");
                    OnAgentDetected?.Invoke(_this.m_core.Cast<MineDeployerInstance>(), agent);
                    return;
                }
            }
            else
            {
                num = _this.m_maxLineDistance;
            }
        }
        
        if (num != _this.DetectionRange)
        {
            _this.UpdateDetectionRange(num);
        }
    }
    
}

[HarmonyPatch(typeof(GenericDamageComponent), nameof(GenericDamageComponent.BulletDamage))]
// Client side bullet damage detection on tripmines
public static class GenericDamageComponent_Patch
{
    public static bool Prefix(GenericDamageComponent __instance, float dam, Agent sourceAgent)
    {
        //Plugin.L.LogInfo($"{nameof(GenericDamageComponent_Patch)} called. GO name:{__instance.name}");

        var mine = __instance.gameObject.GetComponentInParent<MineDeployerInstance>();
        
        //Plugin.L.LogWarning($"Attached to Tripmine: {mine?.name ?? "NULL"}");
        
        //Plugin.L.LogInfo($"{nameof(GenericDamageComponent_Patch)} Dam: {dam}, sourceAgent: {sourceAgent?.name ?? "NULL"}");

        // TODO: Remove this
        //mine.GetController().ApplyHack();
        return false;
    }
}