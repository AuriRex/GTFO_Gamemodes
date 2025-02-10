using System.Collections;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using FX_EffectSystem;
using Gamemodes.Extensions;
using Gamemodes.Net;
using HNS.Core;
using HNS.Extensions;
using HNS.Net;
using Il2CppInterop.Runtime.Attributes;
using Player;
using SNetwork;
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

    private Color _currentColor = Color.white;
    private GMTeam _owningTeam;
    private bool _isConsumable;
    private PlayerAgent Owner => _mine.Owner;
    private CellSoundPlayer Sound => _mine.Sound;
    
    public static readonly Color COL_STATE_DISABLED = new Color(0.2f, 0.2f, 0.2f, 0.3f);
    public static readonly Color COL_STATE_HACKED = new Color(1f, 0.45f, 0.2f, 0.2f);
    
    private static readonly float BARELY_VISIBLE_MOD = 0.0118f;

    public void Start()
    {
        _isConsumable = gameObject.name.StartsWith("Consumable_");
        
        _mine = GetComponent<MineDeployerInstance>();

        _mine.m_detectionEnabled = false;
        
        if (_mine == null)
        {
            Plugin.L.LogError("HELP: MineDeployerInstance is null, this should never happen!");
        }

        if (_isConsumable)
        {
            _mine.GetComponentInChildren<Light>().SafeDestroy();
            if (SNet.IsMaster)
            {
                StartCoroutine(EvilSequence().WrapToIl2Cpp());
            }
            return;
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
        
        var modelGo = transform.Find("TripMine_1(Clone)/Rot/Mine_Deployer_Direct_1")?.gameObject;
        _modelRenderer = modelGo?.GetComponent<Renderer>();
        
        StartCoroutine(DelayedSetup().WrapToIl2Cpp());
    }

    [HideFromIl2Cpp]
    private IEnumerator DelayedSetup()
    {
        yield return null;

        SetStateDisabled();

        yield return new WaitForSeconds(2f);
        
        _mine.Mode = eStickyMineMode.Alarm;
        SetStateDetecting();
    }

    public void Update()
    {
        float baseValue = 1f;
        float blinkSpeedMulti = 1f;
        float blinkAmplitude = 0.25f;
        switch (_currentState)
        {
            default:
                return;
            case MineState.Alarm:
                break;
            case MineState.Hacked:
                baseValue = 0.3f;
                blinkSpeedMulti = 0.25f;
                blinkAmplitude = 0.45f;
                break;
        }
        
        var value = baseValue + Mathf.Sin(Time.time * 20f * blinkSpeedMulti) * blinkAmplitude;

        var col = _currentColor * value;

        _light.Color = col;
        _lineRenderer.startColor = col;
        _lineRenderer.endColor = col;
    }

    public void RefreshVisuals()
    {
        var owner = Owner?.Owner;

        if (owner == null)
        {
            // No owner Mines usually happen due to players disconnecting
            _currentState = MineState.Disabled;
            _mine.m_detectionEnabled = false;
            return;
        }
        
        NetworkingManager.GetPlayerInfo(owner, out var ownerInfo);

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

        _currentColor = color;
        
        _modelRenderer.SetPropertyBlock(block);
        
        _light.Color = color;
        _lineRenderer.startColor = color;
        _lineRenderer.endColor = color;
    }
    
    [HideFromIl2Cpp]
    public static void ProcessIncomingAction(PlayerWrapper sender, MineDeployerInstance mine, MineState mineState)
    {
        Plugin.L.LogDebug($"{nameof(CustomMineController)}.{nameof(ProcessIncomingAction)}: State:{mineState}, Sender:{sender.NickName}, Mine:{mine?.name}");

        var controller = mine.GetController();
        
        switch (mineState)
        {
            default:
            case MineState.DoNotChange:
                break;
            case MineState.Alarm:
                controller.StartAlarmSequence(sender.PlayerAgent);
                break;
            case MineState.Detecting:
                controller.SetStateDetecting();
                break;
            case MineState.Disabled:
                controller.SetStateDisabled();
                break;
            case MineState.Hacked:
                controller.StartHackedSequence(sender.PlayerAgent);
                break;
        }
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
        _sequenceCoroutine = StartCoroutine(routine.WrapToIl2Cpp());
    }
    
    [HideFromIl2Cpp]
    private void SetStateDetecting()
    {
        StopSequenceCoroutine();
        StopDisableRoutine();
        _currentState = MineState.Detecting;
        _mine.m_detectionEnabled = true;
        RefreshVisuals();
    }

    [HideFromIl2Cpp]
    private void SetStateDisabled()
    {
        StopSequenceCoroutine();
        StopDisableRoutine();
        _currentState = MineState.Disabled;
        _mine.m_detectionEnabled = false;
        RefreshVisuals();
    }
    
    [HideFromIl2Cpp]
    private void StartAlarmSequence(PlayerAgent target)
    {
        StopDisableRoutine();
        StartSequenceCoroutine(AlarmSequence(target));
    }

    [HideFromIl2Cpp]
    private void StartHackedSequence(PlayerAgent hacker)
    {
        StopDisableRoutine();
        StartSequenceCoroutine(HackSequence(hacker));
    }

    [HideFromIl2Cpp]
    public void SetDetecting()
    {
        NetSessionManager.SendMineAction(_mine, MineState.Detecting);
    }
    
    [HideFromIl2Cpp]
    public void SetDisabled()
    {
        NetSessionManager.SendMineAction(_mine, MineState.Disabled);
    }
    
    [HideFromIl2Cpp]
    public void DetectedLocalPlayer()
    {
        if (NetworkingManager.GetLocalPlayerInfo().Team == (int)_owningTeam)
            return;
        
        NetSessionManager.SendMineAction(_mine, MineState.Alarm);
    }
    
    [HideFromIl2Cpp]
    public void ApplyHack()
    {
        NetSessionManager.SendMineAction(_mine, MineState.Hacked);
    }
}