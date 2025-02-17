using System.Collections;
using AK;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Core;
using HNS.Core;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace HNS.Components;

public class XRayInstance : MonoBehaviour
{
    private static readonly int ID_COLOR = Shader.PropertyToID("_Color");

    public bool IsAvailable => _currentState == XRayState.Idle;
    
    private XRays _xrays;
    private XRayRenderer _xrayRenderer;
    private CellSoundPlayer _sound;
    
    private readonly int _raysPerSecond = 10_000;
    private readonly float _castDuration = 0.5f;
    
    public float fieldOfView = 60f;
    public float scanMaxDistance = 20f;

    public Color ColorDefault
    {
        get => _xrays.defaultColor;
        set => _xrays.defaultColor = value;
    }

    public Color ColorEnemy
    {
        get => _xrays.enemyColor;
        set => _xrays.enemyColor = value;
    }
    
    private XRayRenderMode _renderMode = XRayRenderMode.Behind;

    private XRayState _currentState;
    
    public float fadeoutMultiplier = 0.05f;
    
    public void Start()
    {
        _xrays = gameObject.AddComponent<XRays>();
        _xrayRenderer = gameObject.AddComponent<XRayRenderer>();
        _xrays.m_renderer = _xrayRenderer;
        _xrayRenderer.material = XRayManager.XRayMaterial;

        _sound = new CellSoundPlayer();
        
        ColorDefault = Color.cyan * 0.8f;
        ColorEnemy = Color.red;
        _xrays.enemySize = 2f;
        _xrays.defaultSize = 1f;
        //_xrays.interactionSize = 0.2f;
        
        SetXRaysActive(false);
    }

    public void OnDestroy()
    {
        _sound.Recycle();
        _sound = null;
    }

    public void Update()
    {
        switch (_currentState)
        {
            case XRayState.Idle:
                return;
            case XRayState.Casting:
                UpdateXRayCasts();
                break;
            case XRayState.Fadeout:
                break;
        }
    }

    public void StartCasting(Vector3 origin, Vector3 direction)
    {
        gameObject.transform.position = origin;
        gameObject.transform.rotation = Quaternion.LookRotation(direction);
        _sound.UpdatePosition(origin);
        _sound.Post(EVENTS.HUD_INFO_TEXT_GENERIC_APPEAR, isGlobal: false);
        SetState(XRayState.Casting);
    }
    
    private void SetState(XRayState state)
    {
        _currentState = state;
        switch (state)
        {
            case XRayState.Idle:
                return;
            case XRayState.Casting:
                OnStartCasting();
                break;
            case XRayState.Fadeout:
                StartCoroutine(FadeXRays().WrapToIl2Cpp());
                break;
        }
    }

    private void OnStartCasting()
    {
        SetXRaysActive(true);
        _xrayRenderer.m_properties?.AddColor(ID_COLOR, Color.white);
        StartCoroutine(Coroutines.DoAfter(_castDuration, () =>
        {
            SetState(XRayState.Fadeout);
        }).WrapToIl2Cpp());
    }

    private void SetXRaysActive(bool active)
    {
        _xrays.enabled = false;
        _xrayRenderer.enabled = active;
        
        if (_xrayRenderer.m_emissionData == null)
        {
            _xrayRenderer.m_emissionData = new Il2CppSystem.Collections.Generic.List<XRayRenderer.DataStruct>(capacity: 0);
        }
    }
    
    private void UpdateXRayCasts()
    {
        int n = Mathf.CeilToInt(_raysPerSecond * Mathf.Min(0.05f, Time.deltaTime));
        _xrays.Cast(n);
        _xrays.fieldOfView = fieldOfView;
        _xrayRenderer.range = scanMaxDistance;
        _xrayRenderer.mode = (int) _renderMode;
    }
    
    [HideFromIl2Cpp]
    private IEnumerator FadeXRays()
    {
        var props = _xrayRenderer.m_properties;

        var value = 1f;
        while (value > 0)
        {
            value -= Time.deltaTime * fadeoutMultiplier;

            var easedValue = EaseOutQuint(value);

            if (easedValue < 0.05f)
                break;
            
            props.AddColor(ID_COLOR, Color.white * easedValue);
            yield return null;
        }
        
        _xrayRenderer.Clear();
        
        SetXRaysActive(false);
        
        SetState(XRayState.Idle);
    }
    
    private static float EaseOutQuint(float x)
    {
        return 1 - Mathf.Pow(1 - x, 5);
    }

    private enum XRayState
    {
        Idle,
        Casting,
        Fadeout
    }
    
    public enum XRayRenderMode
    {
        InFront = 0,
        Behind = 1,
    }
}