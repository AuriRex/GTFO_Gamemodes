using UnityEngine;

namespace HNS.Core;

public class CellSoundPlayerWrapped : ISoundPlayer
{
    private readonly CellSoundPlayer _soundPlayer;

    public CellSoundPlayerWrapped(CellSoundPlayer soundPlayer)
    {
        _soundPlayer = soundPlayer;
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
        _soundPlayer.Post(soundEventID, position);
    }

    public void Activate()
    {
        
    }

    public void Deactivate()
    {
        
    }
}