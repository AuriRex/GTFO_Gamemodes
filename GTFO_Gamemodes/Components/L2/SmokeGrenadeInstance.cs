using AK;
using Feedback;
using GameData;
using UnityEngine;

namespace Gamemodes.Components.L2;

public class SmokeGrenadeInstance : GenericGrenadeInstance, IGrenade
{
    public bool AlertEnemies => false;
    public float NoiseRadius => 50f;
    public uint ExplosionSoundID => EVENTS.BIRTHER_FOG_BALL_APPEAR; // EVENTS.CUTTERFEEDBACK; // <-- too faint
    public bool HideVisualsOnDetonation => false;
    public float FuseTime => 1.5f;
    public float DecayTime => 25f;

    private GameObject _fogSphereGo;
    
    public override void Setup(ItemDataBlock data)
    {
        Grenade = this;
        
        base.Setup(data);
    }

    public void Setup(Rigidbody rigidbody)
    {
        
    }

    public void OnDestroyCleanup()
    {
        if (_fogSphereGo != null)
            Destroy(_fogSphereGo);
    }

    public uint GetBounceSound(int bounceCount)
    {
        return EVENTS.DECOYCANBOUNCE;
    }

    public void Detonate()
    {
        _fogSphereGo = CreateAndPlayFogSphere(transform.position);
    }

    public void OnFixedUpdate()
    {
        
    }

    public void DecayUpdate()
    {
        
    }
    
    private static GameObject CreateAndPlayFogSphere(Vector3 position)
    {
        var go = new GameObject("FogSphere_From_Smoke_Nade");

        go.transform.position = position;

        var fogSphere = go.AddComponent<FogSphereHandler>();

        fogSphere.m_range = 20f;
        fogSphere.m_rangeMax = 20f;
        fogSphere.m_rangeMin = 0f;

        fogSphere.m_totalLength = 15f;

        // Intensity = Color/Light boost?
        //fogSphere.m_intensityMax = 2f;
        fogSphere.m_intensityMin = 0f;
        
        fogSphere.m_density = 2f;
        fogSphere.m_densityMax = 20f;
        fogSphere.m_densityMin = 0f;

        // Second curve, both used for fog density
        fogSphere.m_densityAmountMax = 1f;
        fogSphere.m_densityAmountMin = 0f;

        fogSphere.m_rangeCurve.AddKey(0f, 0f);
        fogSphere.m_rangeCurve.AddKey(0.08f, 20f);
        fogSphere.m_rangeCurve.AddKey(0.5f, 20f);
        fogSphere.m_rangeCurve.AddKey(1f, 12.5f);

        fogSphere.m_densityAmountCurve.AddKey(0f, 1f);
        fogSphere.m_densityAmountCurve.AddKey(0.9f, 1f);
        fogSphere.m_densityAmountCurve.AddKey(1f, 0f);
        
        fogSphere.m_densityCurve.AddKey(0f, 0f);
        fogSphere.m_densityCurve.AddKey(0.01f, 20f);
        fogSphere.m_densityCurve.AddKey(0.05f, 10f);
        fogSphere.m_densityCurve.AddKey(0.5f, 5f);
        fogSphere.m_densityCurve.AddKey(0.8f, 0.5f);
        fogSphere.m_densityCurve.AddKey(0.95f, 0.1f);
        fogSphere.m_densityCurve.AddKey(1f, 0f);
        fogSphere.m_densityCurve.AddKey(2f, 0f);
        
        if (!fogSphere.Play())
        {
            DestroyImmediate(go);
        }

        return go;
    }
}