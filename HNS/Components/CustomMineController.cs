using System.Collections;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using FX_EffectSystem;
using Gamemodes.Extensions;
using Gamemodes.Net;
using HNS.Core;
using HNS.Net;
using Il2CppInterop.Runtime.Attributes;
using Player;
using UnityEngine;

namespace HNS.Components;

public partial class CustomMineController : MonoBehaviour
{
    private Coroutine _coroutine;

    private MineState _currentState = MineState.Detecting;

    private MineDeployerInstance _mine;
    private MineDeployerInstance_Detect_Laser _detection;
    private EffectLight _light;
    private LineRenderer _lineRenderer;
    private Renderer _modelRenderer;

    private GMTeam _owningTeam;
    private PlayerAgent Owner => _mine.Owner;
    
    public static readonly Color COL_STATE_DISABLED = new Color(0.2f, 0.2f, 0.2f, 0.5f);
    public static readonly Color COL_STATE_HACKED = new Color(1f, 0.45f, 0.2f, 0.2f);
    
    private static float BARELY_VISIBLE_MOD = 0.0118f;

    
    public void Start()
    {
        _mine = GetComponent<MineDeployerInstance>();

        if (_mine == null)
        {
            Plugin.L.LogError("HELP: MineDeployerInstance is null, this should never happen!");
        }
        
        var lightAnimator = _mine.GetComponentInChildren<FX_SimplePointLight>();
        _light = lightAnimator.gameObject.GetComponent<EffectLight>();
        lightAnimator.SafeDestroy();
        _light.enabled = true;

        _light.Intensity = 0.002f;
        _light.Color = COL_STATE_DISABLED;

        _detection = _mine.m_detection.Cast<MineDeployerInstance_Detect_Laser>();
        
        _lineRenderer = _detection.m_lineRenderer;
        _lineRenderer.startColor = COL_STATE_DISABLED;
        _lineRenderer.endColor = COL_STATE_DISABLED;

        // Duplicated tripmine model
        transform.Children().FirstOrDefault(child => child.name == "Mine_Deployer_Direct_1")?.gameObject.SetActive(false);

        var modelGo = transform.Find("TripMine_1(Clone)/Rot/Mine_Deployer_Direct_1").gameObject;
        _modelRenderer = modelGo.GetComponent<Renderer>();
        
        StartCoroutine(DelayedSetup().WrapToIl2Cpp());
    }

    [HideFromIl2Cpp]
    private IEnumerator DelayedSetup()
    {
        yield return null;
        
        RefreshVisuals();
    }

    public void RefreshVisuals()
    {
        //Plugin.L.LogWarning($"Owner: {instance.Owner?.name}");

        NetworkingManager.GetPlayerInfo(Owner?.Owner, out var ownerInfo);

        _owningTeam = (GMTeam)ownerInfo.Team;
        
        Color color;
        MaterialPropertyBlock block;
        switch (_owningTeam)
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

        switch (_currentState)
        {
            default:
                break;
            case MineState.Disabled:
                color = COL_STATE_DISABLED;
                block = MPB_DISABLED;
                break;
            case MineState.Hacked:
                color = COL_STATE_HACKED;
                block = MPB_HACKED;
                break;
        }

        if (!ownerInfo.IsLocal)
        {
            var canSeeOwner = ownerInfo.CanBeSeenByLocalPlayer();
        
            var localPlayerInfo = NetworkingManager.GetLocalPlayerInfo();
            var onSameTeamAsMineOwner = ownerInfo.IsOnSameTeamAs(localPlayerInfo);
            var localPlayerIsHider = (GMTeam)localPlayerInfo.Team == GMTeam.Hiders;

            if (!onSameTeamAsMineOwner && (!canSeeOwner || localPlayerIsHider))
            {
                color *= BARELY_VISIBLE_MOD;
            }
        }

        _modelRenderer.SetPropertyBlock(block);
        
        _light.Color = color;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
    }
    
    [HideFromIl2Cpp]
    private void StopCoroutine()
    {
        if (_coroutine == null)
            return;
        
        StopCoroutine(_coroutine);
        _coroutine = null;
    }

    [HideFromIl2Cpp]
    private void StartCoroutine(IEnumerator routine)
    {
        StopCoroutine();
        StartCoroutine(routine.WrapToIl2Cpp());
    }

    [HideFromIl2Cpp]
    public void OnTeamUpdate(GMTeam team)
    {
        _owningTeam = team;
        // TODO
    }
    
    [HideFromIl2Cpp]
    public void StateDetect()
    {
        _mine.m_detectionEnabled = true;
    }

    [HideFromIl2Cpp]
    public void StateDisable()
    {
        _mine.m_detectionEnabled = false;
    }
    
    [HideFromIl2Cpp]
    public void StartAlarmSequence(PlayerAgent target)
    {
        StartCoroutine(AlarmSequence(target));
    }

    [HideFromIl2Cpp]
    public void StartHackedSequence(PlayerAgent hacker)
    {
        StartCoroutine(HackSequence(hacker));
    }
    
    [HideFromIl2Cpp]
    public void DetectedLocalPlayer()
    {
        NetSessionManager.SendMineAction(_mine, MineState.Alarm);
    }
    
    
    #region Sequences
    [HideFromIl2Cpp]
    private IEnumerator AlarmSequence(PlayerAgent target)
    {
        yield return null;
    }
    
    [HideFromIl2Cpp]
    private IEnumerator HackSequence(PlayerAgent hacker)
    {
        yield return null;
    }
    #endregion
}