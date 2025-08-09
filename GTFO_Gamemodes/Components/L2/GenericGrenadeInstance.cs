using System;
using System.Collections.Generic;
using AK;
using Expedition;
using GameData;
using Gamemodes.Extensions;
using SNetwork;
using UnityEngine;

namespace Gamemodes.Components.L2;

public class GenericGrenadeInstance : Item
{
    public const float DEFAULT_FUSE_TIME = 2f;
    public const float DEFAULT_DECAY_TIME = 10f;
    public const float DEFAULT_NOISE_RADIUS = 50f;
    
    private Rigidbody _rigidbody;
    private CellSoundPlayer _sound;
    private bool _hasDetonated;
    private int _bounceCount;
    private float _fuseTime;
    private float _decayTime;

    public IGrenade Grenade { get; set; }
    
    public override void Setup(ItemDataBlock data)
    {
        BaseSetup(data);

        _rigidbody = GetComponent<Rigidbody>();
        _sound = new CellSoundPlayer();

        Grenade?.Setup(_rigidbody);
        
        _fuseTime = Time.time + Grenade?.FuseTime ?? DEFAULT_FUSE_TIME;
    }
    
    private void BaseSetup(ItemDataBlock data)
    {
        this.ItemDataBlock = data;
        this.PublicName = data.publicName;
        base.enabled = true;
        this.PickupInteraction = base.gameObject.GetComponentInChildren<Interact_Base>();
        if (this.PickupInteraction != null)
        {
            this.PickupInteraction.SetupFromItem(this);
        }
        this.m_expeditionGearComponent = base.GetComponent<ExpeditionGear>();
        if (this.m_expeditionGearComponent != null)
        {
            this.m_expeditionGearComponent.Setup();
        }
    }

    public void OnDestroy()
    {
        _sound?.Recycle();
        _sound = null;

        Grenade?.OnDestroyCleanup();
    }

    public override void OnDespawn()
    {
        Destroy(this.gameObject);
    }
    
    public void OnCollisionEnter()
    {
        _bounceCount++;
        var soundId = Grenade?.GetBounceSound(_bounceCount) ?? EVENTS.DECOYCANBOUNCE;
        _sound?.Post(soundId);
    }
    
    public void FixedUpdate()
    {
        Grenade?.OnFixedUpdate();
    }

    public void Update()
    {
        if (!_hasDetonated)
        {
            _sound?.UpdatePosition(transform.position);

            if (Time.time <= _fuseTime)
                return;

            DetonationSequence();
            MakeNoise();

            return;
        }

        Grenade?.DecayUpdate();
        
        if (Time.time < _decayTime)
            return;

        if (!SNet.IsMaster)
            return;
        
        ReplicationWrapper.Replicator.Despawn();
    }

    private void DetonationSequence()
    {
        _hasDetonated = true;

        if (Grenade?.HideVisualsOnDetonation ?? true)
        {
            // Disable model
            foreach (var child in transform.Children())
            {
                child.gameObject.SetActive(false);
            }
        }

        _decayTime = Time.time + Grenade?.DecayTime ?? DEFAULT_DECAY_TIME;
        _rigidbody.velocity = Vector3.zero;
        _rigidbody.angularVelocity = Vector3.zero;
        _rigidbody.useGravity = false;
        
        Grenade?.Detonate();
    }
    
    public void MakeNoise()
    {
        var explosionSoundID = Grenade?.ExplosionSoundID ?? EVENTS.FRAGGRENADEEXPLODE;
        if (explosionSoundID != 0)
            _sound?.Post(explosionSoundID);

        if (!SNet.IsMaster)
            return;

        if (Grenade?.AlertEnemies ?? false)
            return;
        
        HashSet<IntPtr> alreadyListeningEnemies = new();
        var potentialListeners = Physics.OverlapSphere(transform.position, Grenade?.NoiseRadius ?? DEFAULT_NOISE_RADIUS, LayerManager.MASK_ENEMY_DAMAGABLE);
        
        foreach (var collider in potentialListeners)
        {
            var damageable = collider.GetComponent<Dam_EnemyDamageLimb>();
            if (damageable == null)
                continue;

            var listener = damageable.GlueTargetEnemyAgent;
            
            if (listener == null)
                continue;
            
            if (!alreadyListeningEnemies.Add(listener.Pointer))
                continue;
            
            if (Physics.Linecast(transform.position, listener.EyePosition, LayerManager.MASK_WORLD))
            {
                continue;
            }

            var noiseData = new NM_NoiseData()
            {
                position = listener.EyePosition,
                node = listener.CourseNode,
                type = NM_NoiseType.InstaDetect,
                radiusMin = 0.01f,
                radiusMax = 100f,
                yScale = 1f,
                noiseMaker = null,
                raycastFirstNode = false,
                includeToNeightbourAreas = false
            };
            
            NoiseManager.MakeNoise(noiseData);
        }
    }
}