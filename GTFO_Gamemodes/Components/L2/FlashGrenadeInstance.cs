using System;
using System.Collections.Generic;
using AK;
using Enemies;
using GameData;
using Gamemodes.Core;
using Gamemodes.Extensions;
using Player;
using SNetwork;
using UnityEngine;

namespace Gamemodes.Components.L2;

public class FlashGrenadeInstance : GenericGrenadeInstance, IGrenade
{
    public static float LIGHT_INTENSITY_DECAY_RATE = 5f;
    public static float ENEMY_STUN_TICK_INTERVAL = 0.5f;
    public static float ENEMY_STUN_TICK_TIME = 3f;
    
    public static float FORCED_BLINDING_RADIUS = 5f;
    public static float SCREENSHAKE_RADIUS = 50f;
    public static float STUN_RADIUS = 10f;
    
    public static float FLASH_INTENSITY = 10f;
    public static float FLASH_INTENSITY_PHOTOSENSITIVITYMODE = 20f;

    public static Color NEGATIVE_COLOR = new Color(-100, -100, -100, 1);
    
    public bool AlertEnemies => true;
    public float NoiseRadius => 50f;
    public uint ExplosionSoundID => EVENTS.FRAGGRENADEEXPLODE;
    public float FuseTime => 2f;
    public float DecayTime => 10f;

    private readonly List<StunnedEnemy> _stunTargets = new();

    private float _enemyStunTimerEndTime;
    private float _nextEnemyStunTick;
    
    private bool _hasLight;
    private EffectLight _light;
    
    public override void Setup(ItemDataBlock data)
    {
        Grenade = this;
        
        _hasLight = true;
        _light = gameObject.AddComponent<EffectLight>();
        _light.enabled = false;
        _light.GatherAllShadowCasters();
        
        base.Setup(data);
    }

    public void Setup(Rigidbody rigidbody)
    {
        
    }

    public void OnDestroyCleanup()
    {
        ResetStunnedEnemySoundIDs();
        DeallocateFXLight();
    }

    public uint GetBounceSound(int bounceCount)
    {
        return bounceCount <= 1 ? EVENTS.DECOYCANBOUNCEHARD : EVENTS.DECOYCANBOUNCE;
    }

    public void Detonate()
    {
        if (_hasLight)
        {
            _light.Color = GamemodeManager.PhotoSensitivityMode ? NEGATIVE_COLOR : Color.white;
            _light.Range = 40f;
            _light.Intensity = GamemodeManager.PhotoSensitivityMode ? FLASH_INTENSITY_PHOTOSENSITIVITYMODE : FLASH_INTENSITY;
            _light.enabled = true;
        }
        
        var castPosition = transform.position + (Vector3.up * 0.25f);
        
        var localPlayer = PlayerManager.Current.m_localPlayerAgentInLevel;
        var viewPos = localPlayer.FPSCamera.m_camera.WorldToViewportPoint(transform.position);
        var lineOfSightBlocked = Physics.Linecast(castPosition, localPlayer.EyePosition, LayerManager.MASK_WORLD);
        var playerDistance = Vector3.Distance(transform.position, localPlayer.EyePosition);
        
        if (playerDistance < FORCED_BLINDING_RADIUS
            || (viewPos.x > 0 && viewPos.x < 1
                              && viewPos.y > 0 && viewPos.y < 1
                              && viewPos.z > 0
                              && !lineOfSightBlocked))
        {
            FlashBlinder.BlindLocalPlayer();
            localPlayer.FPSCamera.Shake(10f, 2f, 30f);
        }
        else if (playerDistance < SCREENSHAKE_RADIUS)
        {
            var amp = Math.Max(0.1f, (1 - playerDistance / SCREENSHAKE_RADIUS));

            if (!lineOfSightBlocked)
                amp *= 2.5f;
            
            localPlayer.FPSCamera.Shake(0.75f, amp, 20f);
        }
        
        CollectEnemiesToStun();
    }

    public void DecayUpdate()
    {
        UpdateLights();

        if (Time.time < _enemyStunTimerEndTime && Time.time > _nextEnemyStunTick)
        {
            DoEnemyStunTick();
        }
        
        
    }

    public void OnFixedUpdate()
    {
        
    }
    
    
    private void DeallocateFXLight()
    {
        if (!_hasLight)
        {
            return;
        }
        
        Destroy(_light);
        _hasLight = false;
    }
    
    private void UpdateLights()
    {
        if (!_hasLight)
        {
            return;
        }

        var col = GamemodeManager.PhotoSensitivityMode ? NEGATIVE_COLOR : Color.white;
        _light.Color = col * Math.Min(1, _light.Intensity - LIGHT_INTENSITY_DECAY_RATE * 0.5f * Time.deltaTime);
        _light.Range = 40f;
        _light.Intensity = Math.Max(0, _light.Intensity - LIGHT_INTENSITY_DECAY_RATE * Time.deltaTime);
    }

    private void ResetStunnedEnemySoundIDs()
    {
        foreach (var stunnedEnemy in _stunTargets)
        {
            if (stunnedEnemy.HasDiedOrIsDestroyed)
                continue;
            
            EnemyAgent enemy = stunnedEnemy;

            enemy.EnemySFXData.SFX_ID_hurtBig = stunnedEnemy.OriginalHurtBigSoundID;
        }
    }

    private void DoEnemyStunTick()
    {
        _nextEnemyStunTick += ENEMY_STUN_TICK_INTERVAL;

        foreach (var stunnedEnemy in _stunTargets)
        {
            if (stunnedEnemy.HasDiedOrIsDestroyed)
                continue;
            
            EnemyAgent enemy = stunnedEnemy;

            if (enemy == null || enemy.m_isBeingDestroyed || !enemy.Alive || enemy.Damage.IsStuckInGlue)
            {
                stunnedEnemy.HasDiedOrIsDestroyed = true;
                continue;
            }

            stunnedEnemy.CheckSound();
            enemy.EnemySFXData.SFX_ID_hurtBig = 0;
            enemy.Locomotion.Hitreact.ActivateState(ES_HitreactType.Heavy, ImpactDirection.Front, false, Owner, enemy.EyePosition);
        }
    }
    

    public void CollectEnemiesToStun()
    {
        HashSet<IntPtr> stunTargetMemory = new();

        _stunTargets.Clear();

        var losCastPosition = transform.position + (Vector3.up * 0.5f);
        
        var stunTargets = Physics.OverlapSphere(transform.position, STUN_RADIUS, LayerManager.MASK_ENEMY_DAMAGABLE);
        
        foreach (var collider in stunTargets)
        {
            var damageable = collider.GetComponent<Dam_EnemyDamageLimb>();
            
            if (damageable == null)
                continue;

            var stunTarget = damageable.GlueTargetEnemyAgent;

            if (stunTarget == null)
                continue;

            if (!stunTargetMemory.Add(stunTarget.Pointer))
                continue;

            if (Physics.Linecast(losCastPosition, stunTarget.EyePosition, LayerManager.MASK_WORLD))
            {
                continue;
            }

            _stunTargets.Add(stunTarget);

            if (stunTarget.Damage.IsStuckInGlue)
                continue;
            
            var impactDirection = ES_Hitreact.GetDirection(stunTarget.transform, (stunTarget.transform.position - transform.position));
            stunTarget.Locomotion.Hitreact.ActivateState(ES_HitreactType.Heavy, impactDirection, false, Owner, stunTarget.EyePosition);
        }
        
        _enemyStunTimerEndTime = Time.time + ENEMY_STUN_TICK_TIME;
        _nextEnemyStunTick = Time.time + ENEMY_STUN_TICK_INTERVAL;
    }

    private class StunnedEnemy
    {
        public bool HasDiedOrIsDestroyed { get; set; }
        public uint OriginalHurtBigSoundID { get; private set; }
        private readonly EnemyAgent _agent;

        private StunnedEnemy(EnemyAgent agent)
        {
            _agent = agent;
            OriginalHurtBigSoundID = agent.EnemySFXData.SFX_ID_hurtBig;
        }

        public static implicit operator StunnedEnemy(EnemyAgent agent) => new(agent);
        public static implicit operator EnemyAgent(StunnedEnemy stunnedEnemy) => stunnedEnemy?._agent;

        public void CheckSound()
        {
            if (OriginalHurtBigSoundID != 0)
                return;

            var agentHurt = _agent.EnemySFXData.SFX_ID_hurtBig;
            if (agentHurt == 0)
                return;
            
            OriginalHurtBigSoundID = agentHurt;
        }
    }
}