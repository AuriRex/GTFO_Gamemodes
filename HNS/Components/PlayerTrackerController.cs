using AK;
using Gamemodes.Core;
using Gamemodes.Extensions;
using Gamemodes.Net;
using Gear;
using HNS.Core;
using UnityEngine;

namespace HNS.Components;

public class PlayerTrackerController : MonoBehaviour
{
    private EnemyScanner _scanner;
    private EnemyScannerScreen _screen;
    private ProgressBarMeshPanels _progressBar;
    private State _currentState;

    private const float SCAN_DURATION = 0.5f;
    public static float CooldownDuration = 180f;

    private float _scanStartTime;
    private float _scanDuration;
    private float _cooldownStartTime;
    private float _cooldownDuration;
    
    private static readonly Color COL_DISABLED = new Color(0.2f, 0.2f, 0.3f, 0.15f);
    
    public void Start()
    {
        _scanner = GetComponent<EnemyScanner>();
        _screen = _scanner.m_screen;
        _progressBar = _scanner.m_progressBar;

        SetState(State.Disabled);
        
        var info = NetworkingManager.GetLocalPlayerInfo();
        if (info != null)
        {
            OnTeamChange(info, info.Team);
        }

        NetworkingManager.OnPlayerChangedTeams += OnTeamChange;
    }

    public void OnDestroy()
    {
        NetworkingManager.OnPlayerChangedTeams -= OnTeamChange;
    }

    public void Update()
    {
        if (GamemodeManager.CurrentModeId != HideAndSeekMode.MODE_ID)
        {
            this.SafeDestroy();
            return;
        }
        
        switch (_currentState)
        {
            case State.Disabled:
                break;
            case State.Ready:
                if (!_scanner.FireButtonPressed)
                {
                    return;
                }

                _scanner.Sound.Post(EVENTS.HUD_INFO_TEXT_GENERIC_APPEAR, true);
                
                StartScan(SCAN_DURATION, CooldownDuration);
                
                return;
            case State.Scanning:
                if (Clock.Time < _scanStartTime + _scanDuration)
                {
                    float progress = 1f - (Clock.Time - _scanStartTime) / _scanDuration;
                    _progressBar.SetProgress(progress);
                    return;
                }
                
                SetState(State.Recharging);
                return;
            case State.Recharging:
                if (Clock.Time < _cooldownStartTime + _cooldownDuration)
                {
                    float progress = (Clock.Time - _cooldownStartTime) / _cooldownDuration;
                    _progressBar.SetProgress(progress);
                    _screen.SetNoTargetsText($"<color=orange>{progress:P}</color>");
                    return;
                }
                
                _scanner.Sound.Post(EVENTS.BIOTRACKER_RECHARGED, true);
                SetState(State.Ready);
                return;
        }
    }
    
    private void OnTeamChange(PlayerWrapper player, int teamInt)
    {
        if (!player.IsLocal)
            return;

        var team = (GMTeam)teamInt;

        switch (team)
        {
            case GMTeam.PreGameAndOrSpectator:
            case GMTeam.Seekers:
                SetState(State.Ready);
                break;
            case GMTeam.Hiders:
                SetState(State.Disabled);
                break;
        }
    }
    
    private void StartScan(float scanDuration, float cooldownDuration)
    {
        var time = Clock.Time;
        _scanStartTime = time;
        _scanDuration = scanDuration;
        _cooldownStartTime = time + scanDuration;
        _cooldownDuration = cooldownDuration;
        
        SetState(State.Scanning);
    }
    
    private void SetState(State state)
    {
        _currentState = state;
        switch (state)
        {
            case State.Disabled:
                SetColorAndText("<#F00>DISABLED</color>", COL_DISABLED);
                _screen.SetNoTargetsText("<#F40>://ERROR</color>");
                _progressBar.SetProgress(1f);
                break;
            case State.Ready:
                SetColorAndText("Ready to scan", Color.green);
                _progressBar.SetProgress(1f);
                break;
            case State.Scanning:
                SetColorAndText("Scanning ...", Color.magenta);
                OnStartScanning();
                break;
            case State.Recharging:
                SetColorAndText("Recharging ...", Color.red);
                break;
        }
    }

    private void SetColorAndText(string hintText, Color color)
    {
        _screen.SetNoTargetsText("");
        _screen.ResetGuixColor();
        _screen.SetStatusText(hintText);
        _screen.SetGuixColor(color);
    }
    
    private void OnStartScanning()
    {
        XRayManager.SendCastXRays(transform.position, _scanner.Owner.FPSCamera.Forward);
    }

    private enum State
    {
        Ready,
        Scanning,
        Recharging,
        Disabled,
    }
}