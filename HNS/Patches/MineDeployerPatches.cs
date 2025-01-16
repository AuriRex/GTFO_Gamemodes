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
using HNS.Core;
using Player;
using UnityEngine;

namespace HNS.Patches;

[HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.Setup))]
public static class MineDeployerPatches
{
    private static readonly Color COLOR_BARELY_VISIBLE = new Color(0.0118f, 0, 0, 1);
    public static void Postfix(MineDeployerInstance __instance)
    {
        __instance.Mode = eStickyMineMode.Alarm;
        
        var lightAnimator = __instance.GetComponentInChildren<FX_SimplePointLight>();
        var light = lightAnimator.gameObject.GetComponent<EffectLight>();
        lightAnimator.SafeDestroy();
        light.enabled = true;

        light.Intensity = 0.002f;
        light.Color = Color.blue;
        
        var lr = __instance.m_detection.Cast<MineDeployerInstance_Detect_Laser>().m_lineRenderer;
        lr.startColor = COLOR_BARELY_VISIBLE;
        lr.endColor = COLOR_BARELY_VISIBLE;

        __instance.transform.Children().FirstOrDefault(child => child.name == "Mine_Deployer_Direct_1")?.gameObject.SetActive(false);

        __instance.StartCoroutine(LateMineSetup(__instance, light, lr).WrapToIl2Cpp());
    }

    //private static readonly float BARELY_VISIBLE = 0.002f;
    private static readonly float BARELY_VISIBLE_MOD = 0.0118f;

    private static MaterialPropertyBlock _MPB_HIDER;
    private static MaterialPropertyBlock MPB_HIDER
    {
        get
        {
            if (_MPB_HIDER == null)
            {
                _MPB_HIDER = new MaterialPropertyBlock();
                _MPB_HIDER.SetColor("_EmissiveColor", new Color(0f, 0.5f, 1f, 0.2f));
            }

            return _MPB_HIDER;
        }
    }
    private static readonly MaterialPropertyBlock MPB_SEEKER = new MaterialPropertyBlock();
    private static MaterialPropertyBlock _MPB_OTHER;
    private static MaterialPropertyBlock MPB_OTHER
    {
        get
        {
            if (_MPB_OTHER == null)
            {
                _MPB_OTHER = new MaterialPropertyBlock();
                _MPB_OTHER.SetColor("_EmissiveColor", new Color(0.2f, 1f, 0.2f, 0.2f));
            }

            return _MPB_OTHER;
        }
    }
    
    private static IEnumerator LateMineSetup(MineDeployerInstance instance, EffectLight light, LineRenderer lineRenderer)
    {
        yield return null;

        RefreshMineVisuals(instance, light, lineRenderer);
    }

    public static void RefreshMineVisuals(MineDeployerInstance instance, EffectLight light = null, LineRenderer lineRenderer = null)
    {
        if (instance == null)
        {
            return;
        }
        
        if (light == null)
        {
            light = instance.GetComponentInChildren<EffectLight>();
        }

        if (lineRenderer == null)
        {
            lineRenderer = instance.m_detection.Cast<MineDeployerInstance_Detect_Laser>().m_lineRenderer;
        }
        
        //Plugin.L.LogWarning($"Owner: {instance.Owner?.name}");

        NetworkingManager.GetPlayerInfo(instance.Owner?.Owner, out var ownerInfo);

        Color color;
        MaterialPropertyBlock block;
        switch ((GMTeam)ownerInfo.Team)
        {
            case GMTeam.Hiders:
                color = Color.cyan;
                block = MPB_HIDER;
                break;
            case GMTeam.Seekers:
                color = Color.red;
                block = MPB_SEEKER;
                break;
            default:
            case GMTeam.PreGameAndOrSpectator:
                color = Color.green;
                block = MPB_OTHER;
                break;
        }
        
        //var canSeeOwner = ownerInfo.CanBeSeenByLocalPlayer();
        
        var localPlayerInfo = NetworkingManager.GetLocalPlayerInfo();
        var onSameTeamAsMineOwner = ownerInfo.IsOnSameTeamAs(localPlayerInfo);
        //var isHider = (GMTeam)localPlayerInfo.Team == GMTeam.Hiders;

        if (!onSameTeamAsMineOwner)
        {
            color *= BARELY_VISIBLE_MOD;
        }
        
        var modelGO = instance.transform.Find("TripMine_1(Clone)/Rot/Mine_Deployer_Direct_1").gameObject;

        var modelRenderer = modelGO.GetComponent<Renderer>();
        modelRenderer.SetPropertyBlock(block);
        
        light.Color = color;
        lineRenderer.startColor = color;
        lineRenderer.endColor = color;
    }
}

[HarmonyPatch(typeof(MineDeployerInstance_Detect_Laser), nameof(MineDeployerInstance_Detect_Laser.Setup))]
public static class MineDeployerDetection_Patch
{
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
    
    public static void Postfix(MineDeployerInstance_Detect_Laser __instance)
    {
        __instance.m_scanMask = SCAN_MASK;
        __instance.m_enemyMask = ENEMY_MASK;
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