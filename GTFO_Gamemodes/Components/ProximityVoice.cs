using Player;
using UnityEngine;
using PlayerVoiceManager = Gamemodes.Core.Voice.PlayerVoiceManager;

namespace Gamemodes.Components;

public class ProximityVoice : MonoBehaviour
{
    private const float UPDATE_INTERVAL = 0.1f;

    private LocalPlayerAgent _localPlayer;
    private PlayerAgent _playerAgent;
    private float _nextUpdate;
    
    private float _targetVolume = 1f;
    private float _currentVolume = 1f;
    
    private float _audioFalloffEndDistance = 25f;
    private float _audioFalloffStartDistance = 5f;
    private float _interpSpeed = 1f;
    
    public void Start()
    {
        _playerAgent = GetComponent<PlayerAgent>();

        if (_playerAgent == null || _playerAgent.IsLocallyOwned || _playerAgent.Owner.IsBot)
        {
            Destroy(this);
        }

        _localPlayer = PlayerManager.GetLocalPlayerAgent().TryCast<LocalPlayerAgent>();
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
                _currentVolume += _interpSpeed * Time.deltaTime;
            }
            else if (_currentVolume > _targetVolume)
            {
                _currentVolume -= _interpSpeed * Time.deltaTime;
            }

            if (_targetVolume <= 0 && _currentVolume <= 0.0015f)
                _currentVolume = 0f;
            
            if (_targetVolume >= 1f && _currentVolume >= _targetVolume - 0.0015f)
                _currentVolume = 1f;
            
            PlayerVoiceManager.SetVolume(_playerAgent, _currentVolume);
        }
    }

    private void DoUpdate()
    {
        if (_localPlayer == null)
            return;
        
        var distance = Vector3.Distance(_localPlayer.transform.position, _playerAgent.transform.position);
        
        var value = (Mathf.Clamp(distance, _audioFalloffStartDistance, _audioFalloffEndDistance) - _audioFalloffStartDistance) / (_audioFalloffEndDistance - _audioFalloffStartDistance);

        value = (value - 1f) * -1f;

        _targetVolume = value;
    }

    public PlayerChatManager.RemotePlayerVoiceSettings Settings
    {
        get
        {
            if (_playerAgent == null || PlayerChatManager.Current == null)
            {
                return default;
            }
            return PlayerChatManager.Current.GetRemotePlayerVoiceSettings(_playerAgent.Owner.Lookup);
        }
        set
        {
            if (_playerAgent == null || PlayerChatManager.Current == null || _playerAgent.Owner.Lookup == 0L)
            {
                return;
            }
            PlayerChatManager.Current.SetRemotePlayerVoiceSettings(_playerAgent.Owner.Lookup, value);
        }
    }
}