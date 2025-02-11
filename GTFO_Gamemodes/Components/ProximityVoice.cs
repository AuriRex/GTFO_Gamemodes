using Player;
using UnityEngine;
using PlayerVoiceManager = Gamemodes.Core.Voice.PlayerVoiceManager;

namespace Gamemodes.Components;

public class ProximityVoice : MonoBehaviour
{
    private const float UPDATE_INTERVAL = 0.1f;

    public static float NoLOSMultiplier = 0.75f;
    public static float HasLOSMultiplier = 1f;
    
    public float audioFalloffEndDistance = 25f;
    public float audioFalloffStartDistance = 5f;
    public float interpSpeed = 1f;
    
    private LocalPlayerAgent _localPlayer;
    private PlayerAgent _playerAgent;
    private float _nextUpdate;
    
    private float _targetVolume = 1f;
    private float _currentVolume = 1f;
    
    private bool _changed;
    private float _nodeDistanceMultiplier = 1f;
    private float _rayMultiplier = 1f;
    
    public void Start()
    {
        _playerAgent = GetComponent<PlayerAgent>();

        if (_playerAgent == null || _playerAgent.IsLocallyOwned)
        {
            Destroy(this);
        }
        
#if !DEBUG
        if (_playerAgent.Owner.IsBot)
        {
            Destroy(this);
        }
#endif

        _localPlayer = PlayerManager.GetLocalPlayerAgent().TryCast<LocalPlayerAgent>();
        
        _nextUpdate = Time.realtimeSinceStartup + Random.Range(0f, 1f);
    }

    public void Update()
    {
        var time = Time.realtimeSinceStartup;

        if (time >= _nextUpdate)
        {
            _nextUpdate = time + UPDATE_INTERVAL;

            DoUpdate();
        }

        if (Mathf.Abs(_targetVolume - _currentVolume) > 0.001f)
        {
            if (_currentVolume < _targetVolume)
            {
                _currentVolume += interpSpeed * Time.deltaTime;
            }
            else if (_currentVolume > _targetVolume)
            {
                _currentVolume -= interpSpeed * Time.deltaTime;
            }

            if (_targetVolume <= 0 && _currentVolume <= 0.0015f)
                _currentVolume = 0f;
            
            if (_targetVolume >= 1f && _currentVolume >= _targetVolume - 0.0015f)
                _currentVolume = 1f;

            _changed = true;
        }

        if (_changed)
        {
            _changed = false;
#if DEBUG
            if (_playerAgent.Owner.IsBot)
                return;
#endif
            PlayerVoiceManager.SetVolume(_playerAgent, _currentVolume);
        }
    }

    private void DoUpdate()
    {
        if (_localPlayer == null)
            return;
        
        var distance = Vector3.Distance(_localPlayer.transform.position, _playerAgent.transform.position);
        
        var value = (Mathf.Clamp(distance, audioFalloffStartDistance, audioFalloffEndDistance) - audioFalloffStartDistance) / (audioFalloffEndDistance - audioFalloffStartDistance);

        value = (value - 1f) * -1f;

        _targetVolume = value;

        SetNodeDistanceMulti();

        SetRayMulti(distance);
        
        _targetVolume *= _nodeDistanceMultiplier * _rayMultiplier;
    }

    private void SetNodeDistanceMulti()
    {
        NodeDistance.GetDistance(_playerAgent.Owner.Lookup, out var nodeDistance);

        if (_localPlayer.DimensionIndex != _playerAgent.DimensionIndex)
        {
            _nodeDistanceMultiplier = 0f;
            return;
        }

        _nodeDistanceMultiplier = nodeDistance switch
        {
            0 => 1f,
            1 => 0.8f,
            2 => 0.2f,
            _ => 0.1f
        };
    }
    
    private void SetRayMulti(float distance)
    {
        if (distance >= audioFalloffEndDistance)
            return;

        if (_nodeDistanceMultiplier <= 0)
            return;

        var remotePos = _playerAgent.Position + Vector3.up * 1.75f;

        var direction = (_localPlayer.FPSCamera.Position - remotePos).normalized;

        if (Physics.Raycast(remotePos, direction, out var hit, Mathf.Min(distance, audioFalloffEndDistance),
                LayerManager.MASK_WORLD))
        {
            if (Mathf.Abs(hit.distance - distance) >= 0.1f)
            {
                // distance mismatch
                _rayMultiplier = NoLOSMultiplier;
                return;
            }
        }

        _rayMultiplier = HasLOSMultiplier;
    }
}