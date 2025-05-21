using UnityEngine;

namespace HNS.Core;

public interface ISoundPlayer
{
    void Stop();
    void Post(uint soundEventID);
    void Post(uint soundEventID, Vector3 position);
    void Activate();
    void Deactivate();
}