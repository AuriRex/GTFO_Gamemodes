using System;
using System.Linq;
using CullingSystem;
using Gamemodes.Extensions;
using Gamemodes.Net;
using HNS.Core;
using Player;
using UnityEngine;

namespace HNS.Components;

public class SpectatorController : MonoBehaviour
{
    public static bool IsActive;
    
    private LocalPlayerAgent _localPlayer;
    private FPSCamera _fpsCamera;

    private PlayerWrapper _target;

    private int _targetIndex = 0;
    private Vector3 _targetLookDir;
    private float _zoom = 0;
    private bool _orbitMode;
    private Vector3 _orbitLookDirection = Vector3.forward;

    private readonly eFocusState _inputFilter = eFocusState.FPS;
    private Vector2 _lookInputMouse = Vector2.zero;
    private ValueSmoother _mouseSmoother;
    private float _lookSpeedMouse = 3f;
    private float _yaw;
    private float _pitch;
    private readonly Vector2 _pitchLimit = new(-89, 89);
    private FirstPersonItemHolder _fpsItemHolder;
    private GameObject _guiCrosshairLayer;
    private GameObject _guiPlayerInventory;

    private const float SCROLL_SENSITIVITY = 0.1f;
    
    private static PlayerWrapper[] SpectateablePlayers => NetworkingManager.AllValidPlayers
        .Where(info => !info.IsLocal && info.CanBeSeenByLocalPlayer()).ToArray();
    
    public static bool TryEnter(PlayerWrapper target = null)
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return false;

        if (IsActive)
            return false;

        if (target == null)
        {
            if (SpectateablePlayers.Length == 0)
                return false;
            
            target = SpectateablePlayers[0];
        }
        
        var spectatorController = localPlayer.FPSCamera.gameObject.GetOrAddComponent<SpectatorController>();
        
        spectatorController.Spectate(target);
        return true;
    }

    public static void TryExit()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return;
        
        localPlayer.FPSCamera.gameObject.GetComponent<SpectatorController>()?.SafeDestroy();
    }

    public void Awake()
    {
        if (IsActive || !PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
        {
            Destroy(this);
            return;
        }

        IsActive = true;
        
        _mouseSmoother = ValueSmoother.Create(ValueSmoother.SmootherType.Vec2, 8, 0.2f);
        
        _localPlayer = localPlayer.Cast<LocalPlayerAgent>();
        _fpsCamera = _localPlayer.FPSCamera;
        
        _localPlayer.Locomotion.enabled = false;

        _fpsCamera.MouseLookEnabled = false;
        _fpsCamera.PlayerAgentRotationEnabled = false;
        _fpsCamera.PlayerMoveEnabled = false;

        _localPlayer.Inventory.enabled = false;
        _fpsItemHolder = _localPlayer.FPSCamera.m_holder.GetComponentInChildren<FirstPersonItemHolder>();
        _fpsItemHolder.gameObject.SetActive(false);
        
        // Gets re-enabled on focus state change
        //GuiManager.PlayerLayer.Inventory.gameObject.SetActive(false);

        _guiCrosshairLayer = GuiManager.CrosshairLayer.CanvasTrans.gameObject;
        _guiPlayerInventory = GuiManager.PlayerLayer.Inventory.gameObject;
        
        _localPlayer.PlayerSyncModel.gameObject.SetActive(false);

        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;
    }

    public void OnDestroy()
    {
        NetworkingManager.OnPlayerChangedTeams -= OnPlayerChangedTeams;
        
        IsActive = false;

        if (_target?.PlayerAgent?.PlayerSyncModel != null)
        {
            SetModelPartsActive(_target.PlayerAgent.PlayerSyncModel, true);
        }
        
        if (_localPlayer == null || _fpsCamera == null)
            return;
        
        _localPlayer.Locomotion.enabled = true;
        
        _fpsCamera.MouseLookEnabled = true;
        _fpsCamera.PlayerAgentRotationEnabled = true;
        _fpsCamera.PlayerMoveEnabled = true;
        
        _localPlayer.m_movingCuller.UpdatePosition(_localPlayer.m_dimensionIndex, _localPlayer.Position);
        _localPlayer.m_movingCuller.SetCurrentNode(_localPlayer.CourseNode.m_cullNode);
        
        _localPlayer.Inventory.enabled = true;
        _fpsItemHolder.gameObject.SetActive(true);
        
        //GuiManager.PlayerLayer.Inventory.gameObject.SetActive(true);
        
        _localPlayer.PlayerSyncModel.gameObject.SetActive(true);
        
        _guiCrosshairLayer.SetActive(true);
        _guiPlayerInventory.SetActive(true);
        
        var isSeeker = TeamHelper.IsSeeker(NetworkingManager.LocalPlayerTeam);
        HideAndSeekMode.SetLocalPlayerStatusUIElementsActive(isSeeker);
        HideAndSeekMode.SetNearDeathAudioLimit(_localPlayer, !isSeeker);
        HideAndSeekMode.SetDisplayedLocalPlayerHealth(_localPlayer.Damage.GetHealthRel());
    }

    private void OnPlayerChangedTeams(PlayerWrapper player, int teamInt)
    {
        if (player.IsLocal)
        {
            Destroy(this);
            return;
        }
        
        _targetIndex = Array.IndexOf(SpectateablePlayers, _target);
        
        UpdateSwitchTargetInput(force: true);
    }
    
    public void Update()
    {
        if (CheckExitKeybind())
        {
            return;
        }
        
        UpdateScrollInput();
        UpdateSwitchTargetInput();

        if (_orbitMode)
        {
            MouseLookUpdate(InputMapper.GetAxis.Invoke(InputAction.LookHorizontal, _inputFilter),
                InputMapper.GetAxis.Invoke(InputAction.LookVertical, _inputFilter));
        }

        UpdatePosition();
        UpdateCuller();

        if (!_guiCrosshairLayer.active)
            return;

        _guiCrosshairLayer.SetActive(false);
        _guiPlayerInventory.SetActive(false);
    }

    private bool CheckExitKeybind()
    {
        if (!Input.GetKeyDown(KeyCode.Backspace))
            return false;
        
        if (PlayerChatManager.InChatMode)
            return false;
        
        var state = FocusStateManager.CurrentState;
        
        switch (state)
        {
            case eFocusState.Map:
            case eFocusState.Freeflight:
            case eFocusState.ComputerTerminal:
            case eFocusState.FPS_CommunicationDialog:
            case eFocusState.FPS_TypingInChat:
                return false;
            default:
                Destroy(this);
                return true;
        }
    }

    public void LateUpdate()
    {
        if (_target == null)
            return;

        var healthRel = _target.PlayerAgent.Damage.Health / _target.PlayerAgent.Damage.HealthMax;

        HideAndSeekMode.SetDisplayedLocalPlayerHealth(healthRel);
    }

    private void UpdateSwitchTargetInput(bool force = false)
    {
        var targetChanged = false;
        if (InputMapper.GetButtonDown.Invoke(InputAction.Fire, _inputFilter))
        {
            _targetIndex++;
            targetChanged = true;
        }

        if (InputMapper.GetButtonDown.Invoke(InputAction.Aim, _inputFilter))
        {
            _targetIndex--;
            targetChanged = true;
        }

        if (!targetChanged && !force)
            return;

        var players = SpectateablePlayers;

        if (players.Length == 0)
        {
            Plugin.L.LogDebug($"[{nameof(SpectatorController)}] No targets to spectate, exiting.");
            Destroy(this);
            return;
        }
        
        if (_targetIndex < 0)
        {
            _targetIndex = players.Length - 1;
        }
        
        if (_targetIndex >= players.Length)
        {
            _targetIndex = 0;
        }

        Spectate(players[_targetIndex]);
    }
    
    private void UpdateScrollInput()
    {
        if (FocusStateManager.CurrentState != eFocusState.FPS)
            return;
        
        var scrollDelta = Input.mouseScrollDelta.y * SCROLL_SENSITIVITY * -1;
        
        if (Mathf.Approximately(scrollDelta, 0f))
        {
            return;
        }

        _zoom += scrollDelta;
        
        _zoom = Mathf.Clamp(_zoom, 0, 15);

        if (!_orbitMode && _zoom > 0.01f)
        {
            EnterOrbitMode();
            return;
        }
        
        if (_orbitMode && _zoom < 0.01f)
        {
            ExitOrbitMode();
        }
    }

    private void MouseLookUpdate(float axisHor, float axisVer)
    {
        _lookInputMouse.x = axisHor;
        _lookInputMouse.y = axisVer * LookCameraController.LookYSign;
        _lookInputMouse = _mouseSmoother.Smooth(_lookInputMouse);
        _lookInputMouse *= _lookSpeedMouse * CellSettingsManager.SettingsData.Gameplay.LookSpeed.Value;
        _yaw += _lookInputMouse.x;
        _pitch -= _lookInputMouse.y;
        _yaw = ((_yaw < -360f) ? (_yaw += 360f) : _yaw);
        _yaw = ((_yaw > 360f) ? (_yaw -= 360f) : _yaw);
        //_yaw = Mathf.Clamp(_yaw, _yawLimit.x, _yawLimit.y);
        _pitch = ((_pitch < -360f) ? (_pitch += 360f) : _pitch);
        _pitch = ((_pitch > 360f) ? (_pitch -= 360f) : _pitch);
        _pitch = Mathf.Clamp(_pitch, _pitchLimit.x, _pitchLimit.y);
        //m_pitch = Mathf.Clamp(m_pitch, -m_pitchLimit.x, -m_pitchLimit.y);
    }
    
    private void Spectate(PlayerWrapper player)
    {
        if (player == null)
            return;
        
        var prevTarget = _target;

        if (prevTarget == player)
            return;
        
        _target = player;

        Plugin.L.LogDebug($"[{nameof(SpectatorController)}] Target changed to: {_target.NickName}");
        if (!_target.IsLocal && !_orbitMode)
        {
            _targetLookDir = _target.PlayerAgent.Sync.m_locomotionData.LookDir.Value;
            SetModelPartsActive(_target.PlayerAgent.PlayerSyncModel, false);
        }

        var isSeeker = TeamHelper.IsSeeker(_target.Team);
        HideAndSeekMode.SetLocalPlayerStatusUIElementsActive(isSeeker);
        HideAndSeekMode.SetNearDeathAudioLimit(_localPlayer, !isSeeker);
        
        if (prevTarget == null || !prevTarget.HasAgent)
            return;

        SetModelPartsActive(prevTarget.PlayerAgent.PlayerSyncModel, true);
    }

    private static void SetModelPartsActive(PlayerSyncModelData syncModel, bool active)
    {
        foreach (var headPart in syncModel.m_gfxHead)
        {
            if (headPart == null)
                continue;
            headPart.SetActive(active);
        }
        
        foreach (var torsoPart in syncModel.m_gfxTorso)
        {
            if (torsoPart == null)
                continue;
            torsoPart.SetActive(active);
        }
    }

    private void EnterOrbitMode()
    {
        _orbitMode = true;
        _orbitLookDirection = _targetLookDir;
        
        var angles = Quaternion.LookRotation(_targetLookDir).eulerAngles;
        _pitch = angles.x;
        _yaw = angles.y;
        
        SetModelPartsActive(_target.PlayerAgent.PlayerSyncModel, true);
    }
    
    private void ExitOrbitMode()
    {
        _orbitMode = false;
        
        SetModelPartsActive(_target.PlayerAgent.PlayerSyncModel, false);
    }

    private Vector3 GetTargetPlayerPosition()
    {
        return _target?.PlayerAgent?.Position ?? transform.position + Vector3.up * 0.1f; // _target.position
    }

    private void UpdatePosition()
    {
        if (_target == null || !_target.HasAgent)
            return;

        if (_target.IsLocal)
            return;
        
        var agent = _target.PlayerAgent;

        var cameraPosition = agent.EyePosition;
        var lookDir = agent.Sync.m_locomotionData.LookDir.Value;

        if (_orbitMode)
        {
            _orbitLookDirection = Quaternion.Euler(_pitch, _yaw, 0f) * Vector3.forward;
            
            cameraPosition = agent.EyePosition - _orbitLookDirection * _zoom;
            _targetLookDir = _orbitLookDirection;
        }
        else
        {
            _targetLookDir = Vector3.Slerp(_targetLookDir, lookDir, Clock.Delta * 10f);
        }

        _fpsCamera.OverridePositionAndRotation(cameraPosition, Quaternion.LookRotation(_targetLookDir));
    }
    
    private void UpdateCuller()
    {
        var cullPos = GetTargetPlayerPosition();
        
        if (Physics.Raycast(cullPos, Vector3.down, out var hit, 50f, LayerManager.MASK_WORLD))
        {
            cullPos = hit.m_Point;
        }
            
        _localPlayer.m_movingCuller.UpdatePosition(_localPlayer.m_dimensionIndex, cullPos);

        if (_target == null || _target.IsLocal)
            return;
        
        if (_localPlayer.m_movingCuller.CurrentNode.Pointer != _target.PlayerAgent.CourseNode.m_cullNode.Pointer)
            _localPlayer.m_movingCuller.SetCurrentNode(_target.PlayerAgent.CourseNode.m_cullNode);
    }
}