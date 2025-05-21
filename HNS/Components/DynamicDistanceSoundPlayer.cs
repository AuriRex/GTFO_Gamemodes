using HNS.Core;
using Player;
using UnityEngine;

namespace HNS.Components;

public class DynamicDistanceSoundPlayer : MonoBehaviour, ISoundPlayer
{
    private CellSoundPlayer _soundPlayer;
    private LocalPlayerAgent _localPlayer;
    private Transform _target;
    private float _maxDistance;
    private bool _destroyed;

    public void Awake()
    {
        if (_destroyed)
            return;
        
        _soundPlayer ??= new CellSoundPlayer();
        _localPlayer = PlayerManager.GetLocalPlayerAgent().Cast<LocalPlayerAgent>();
    }

    public void SetTarget(Transform target, float maxDistance)
    {
        _target = target;
        _maxDistance = maxDistance;
        
        UpdateSoundPosition();
    }

    private void UpdateSoundPosition()
    {
        var targetVector = _target.position - _localPlayer.CamPos;
        var distance = targetVector.magnitude;

        if (distance > _maxDistance)
        {
            targetVector = targetVector.normalized * _maxDistance;
        }
        
        var soundPos = _localPlayer.CamPos + targetVector;
        _soundPlayer.UpdatePosition(soundPos);
        //Fig.DrawLine(soundPos, soundPos + Vector3.up);
    }

    public void Update()
    {
        UpdateSoundPosition();
    }
    
    public void OnDestroy()
    {
        _soundPlayer.Recycle();
        _soundPlayer = null;
        _destroyed = true;
    }

    public void Stop()
    {
        _soundPlayer.Stop();
    }

    public void Post(uint soundEventID)
    {
        _soundPlayer.Post(soundEventID);
    }

    public void Post(uint soundEventID, Vector3 position)
    {
        _soundPlayer.Post(soundEventID);
    }

    public void Activate()
    {
        enabled = true;
    }

    public void Deactivate()
    {
        enabled = false;
    }
}