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
    private Coroutine _sequenceCoroutine;
    private Coroutine _disableCoroutine;

    private MineState _currentState = MineState.Detecting;

    private MineDeployerInstance _mine;
    private MineDeployerInstance_Detect_Laser _detection;
    private EffectLight _light;
    private LineRenderer _lineRenderer;
    private Renderer _modelRenderer;

    private GMTeam _owningTeam;
    private PlayerAgent Owner => _mine.Owner;
    private CellSoundPlayer Sound => _mine.Sound;
    
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

    public void Update()
    {
        switch (_currentState)
        {
            default:
                return;
            case MineState.Alarm:
            case MineState.Hacked:
                break;
        }
        
        var value = 1f + Mathf.Sin(Time.time * 20f) * 0.125f;

        _light.Color *= value;
        _lineRenderer.startColor = _light.Color;
        _lineRenderer.endColor = _light.Color;
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
            case MineState.Alarm:
                break;
            case MineState.Disabled:
                color = COL_STATE_DISABLED;
                block = MPB_DISABLED;
                break;
            case MineState.Hacked:
                color = COL_STATE_HACKED;
                block = MPB_HACKED;
                break;
            default:
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
                break;
        }

        _modelRenderer.SetPropertyBlock(block);
        
        _light.Color = color;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
    }
    
    [HideFromIl2Cpp]
    private void DisableMineForSeconds(float duration)
    {
        StopDisableRoutine();
        
        _disableCoroutine = StartCoroutine(DisableRoutine(duration).WrapToIl2Cpp());
        return;

        IEnumerator DisableRoutine(float disableDuration)
        {
            _currentState = MineState.Disabled;
            _mine.m_detectionEnabled = false;
            RefreshVisuals();
            yield return new WaitForSeconds(disableDuration);
            if (_mine == null || _mine.WasCollected)
                yield break;
            _mine.m_detectionEnabled = true;
            _currentState = MineState.Detecting;
            RefreshVisuals();
            _disableCoroutine = null;
        }
    }

    private void StopDisableRoutine()
    {
        if (_disableCoroutine == null)
        {
            return;
        }

        StopCoroutine(_disableCoroutine);
        _disableCoroutine = null;
    }
    
    [HideFromIl2Cpp]
    private void StopSequenceCoroutine()
    {
        if (_sequenceCoroutine == null)
            return;
        
        StopCoroutine(_sequenceCoroutine);
        _sequenceCoroutine = null;
    }

    [HideFromIl2Cpp]
    private void StartSequenceCoroutine(IEnumerator routine)
    {
        StopSequenceCoroutine();
        StartCoroutine(routine.WrapToIl2Cpp());
    }
    
    [HideFromIl2Cpp]
    public void StateDetect()
    {
        StopSequenceCoroutine();
        StopDisableRoutine();
        _currentState = MineState.Detecting;
        _mine.m_detectionEnabled = true;
        RefreshVisuals();
    }

    [HideFromIl2Cpp]
    public void StateDisable()
    {
        StopSequenceCoroutine();
        StopDisableRoutine();
        _currentState = MineState.Disabled;
        _mine.m_detectionEnabled = false;
        RefreshVisuals();
    }
    
    [HideFromIl2Cpp]
    public void StartAlarmSequence(PlayerAgent target)
    {
        StartSequenceCoroutine(AlarmSequence(target));
    }

    [HideFromIl2Cpp]
    public void StartHackedSequence(PlayerAgent hacker)
    {
        StartSequenceCoroutine(HackSequence(hacker));
    }
    
    [HideFromIl2Cpp]
    public void DetectedLocalPlayer()
    {
        if (NetworkingManager.GetLocalPlayerInfo().Team == (int)_owningTeam)
            return;
        
        NetSessionManager.SendMineAction(_mine, MineState.Alarm);
    }
    
    [HideFromIl2Cpp]
    private IEnumerator PosHighlighter(Vector3 position, float displayDuration, float fadeoutDuration = 5f)
    {
        if (displayDuration == 0 && fadeoutDuration == 0)
            yield break;
        
        var go = new GameObject("HNS_Temp_Highlight_GO");
        go.transform.position = position;
        
        var marker = go.AddComponent<PlaceNavMarkerOnGO>();

        marker.type = PlaceNavMarkerOnGO.eMarkerType.Waypoint;
        marker.m_nameToShow = "<color=orange><b>Motion Detected!</b></color>";
        
        var color = Color.red;
        if (_owningTeam == GMTeam.Seekers)
            color = Color.cyan;
        
        marker.PlaceMarker();
        marker.UpdatePlayerColor(color);
        marker.SetMarkerVisible(true);
        
        if (displayDuration > 0)
            yield return new WaitForSeconds(displayDuration);

        if (fadeoutDuration > 0)
        {
            marker.m_marker.FadeOut(0f, fadeoutDuration);
        
            yield return new WaitForSeconds(fadeoutDuration + 0.1f);
        }
        
        marker.SafeDestroyGameObject();
    }
    
    
    
    #region Sequences
    [HideFromIl2Cpp]
    private IEnumerator AlarmSequence(PlayerAgent target)
    {
        float disableDuration = 3f;
        if (_owningTeam == GMTeam.Hiders)
            disableDuration = 30f;
        
        DisableMineForSeconds(disableDuration);
        
        _currentState = MineState.Alarm;
        RefreshVisuals();

        StartCoroutine(PosHighlighter(target.Position + Vector3.up, 3f).WrapToIl2Cpp());
        
        for (int i = 0; i < 3; i++)
        {
            Sound.Stop();
            Sound.Post(AK.EVENTS.HACKING_PUZZLE_LOCK_ALARM, transform.position);
            yield return new WaitForSeconds(0.75f);
        }

        //yield return new WaitForSeconds(0.75f);
        
        _currentState = MineState.Disabled;
        RefreshVisuals();
        yield return null;
    }

    [HideFromIl2Cpp]
    private IEnumerator HackSequence(PlayerAgent hacker)
    {
        yield return null;
    }
    #endregion
}