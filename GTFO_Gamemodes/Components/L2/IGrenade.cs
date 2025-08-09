using UnityEngine;

namespace Gamemodes.Components.L2;

public interface IGrenade
{
    /// <summary>
    /// The time it takes for the grenade to detonate.
    /// </summary>
    float FuseTime => GenericGrenadeInstance.DEFAULT_FUSE_TIME;
    
    /// <summary>
    /// The time until the grenade instance gets despawned <i>after</i> it has detonated.
    /// </summary>
    float DecayTime => GenericGrenadeInstance.DEFAULT_DECAY_TIME;

    /// <summary>
    /// Should the visuals be hidden upon detonation?
    /// </summary>
    bool HideVisualsOnDetonation => true;
    
    /// <summary>
    /// Should the exploding grenade alert nearby enemies?
    /// </summary>
    bool AlertEnemies => true;
    
    /// <summary>
    /// All enemies in this radius will get alerted if <see cref="AlertEnemies"/> is <c>True</c>
    /// </summary>
    float NoiseRadius => 50f;
    
    /// <summary>
    /// The sound event to play upon detonation.<br/>
    /// Set to <c>0</c> to not play any sound.
    /// </summary>
    uint ExplosionSoundID => AK.EVENTS.FRAGGRENADEEXPLODE;

    void Setup(Rigidbody rigidbody);
    
    void OnDestroyCleanup();
    
    uint GetBounceSound(int bounceCount);

    void Detonate();
    
    void OnFixedUpdate();
    
    void DecayUpdate();
}