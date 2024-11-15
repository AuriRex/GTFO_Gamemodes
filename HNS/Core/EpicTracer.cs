using System;
using System.Collections;
using System.Linq;
using Gamemodes.Extensions;
using HNS.Net.Packets;
using UnityEngine;

namespace HNS.Core;

public class EpicTracer
{
    private const float EPIC_TRACER_TIME = 1.2f;
    private static readonly Color TRACER_COLOR = Color.red;
    
    public static IEnumerator EpicTracerRoutine(pEpicTracer data)
    {
        var max = EPIC_TRACER_TIME;
        var duration = max;

        if (!TryGetFreeMaterial(out var matWrapper))
        {
            matWrapper = GetOldestFigMat();
        }

        matWrapper.IsInUse = true;
        var id = matWrapper.Frame;
        var mat = matWrapper.Material;
        while (duration > 0)
        {
            if (id != matWrapper.Frame)
                yield break;
            
            var normalized = duration / max;

            var size = Mathf.Clamp(EaseInExpo(normalized), 0.01f, 1f);
            
            var eased = EaseOutCubic(normalized);
            var opacity = eased * 2000f;
            
            mat.SetFloat(ID_SHADER_PROPERTY_OPACITY, opacity);
            mat.SetFloat(ID_SHADER_PROPERTY_WORLD_SIZE, size);
            
            //Plugin.L.LogDebug($"N:{normalized}, Eased:{eased} => {value}");
            
            Fig.DrawLine(data.Origin, data.Destination, TRACER_COLOR, mat);
            duration -= Time.deltaTime;
            yield return null;
        }
        
        matWrapper.IsInUse = false;
    }
    
    // https://easings.net/
    private static float EaseOutCubic(float x)
    {
        return 1f - Mathf.Pow(1f - x, 3f);
    }
    
    private static float EaseInExpo(float x)
    {
        return x == 0 ? 0 : Mathf.Pow(2, 10 * x - 10);
    }

    private static FigMatWrapper[] _pool;
    private const int POOL_SIZE = 8;
    private static void TrySetupFigMatPool()
    {
        if (_pool != null)
            return;

        _pool = new FigMatWrapper[POOL_SIZE];

        for (int i = 0; i < POOL_SIZE; i++)
        {
            _pool[i] = CreateAndRegisterNew();
        }
    }

    private static bool TryGetFreeMaterial(out FigMatWrapper matWrapper)
    {
        TrySetupFigMatPool();
        
        matWrapper = _pool.FirstOrDefault(i => !i.IsInUse);
        return matWrapper != null;
    }

    private static FigMatWrapper GetOldestFigMat()
    {
        return _pool.OrderBy(i => i.Frame).First();
    }
    
    public const string SHADER_NAME = "UI/Fig";

    public const string SHADER_PROPERTY_OPACITY = "_Opacity"; // 0 .. 1
    public const string SHADER_PROPERTY_SOFTNESS = "_Softness"; // 0 .. 1
    public const string SHADER_PROPERTY_DOTCOUNT = "_DotCount"; // 0 .. 1000
    public const string SHADER_PROPERTY_DOTSNAP = "_DotSnap";
    public const string SHADER_PROPERTY_ZTEST = "_ZTest";
    public const string SHADER_PROPERTY_WORLD_SIZE = "_WorldSize";

    public static Shader SHADER_UI_FIG { get; private set; }

    public static int ID_SHADER_PROPERTY_OPACITY { get; private set; }
    public static int ID_SHADER_PROPERTY_SOFTNESS { get; private set; }
    public static int ID_SHADER_PROPERTY_DOTCOUNT { get; private set; }
    public static int ID_SHADER_PROPERTY_DOTSNAP { get; private set; }
    public static int ID_SHADER_PROPERTY_ZTEST { get; private set; }
    public static int ID_SHADER_PROPERTY_WORLD_SIZE { get; private set; }
    
    private static void SetupShaderAndCacheProperties()
    {
        if (ID_SHADER_PROPERTY_OPACITY != 0)
            return;

        SHADER_UI_FIG = Shader.Find(SHADER_NAME);

        SHADER_UI_FIG.DontDestroyAndSetHideFlags();

        ID_SHADER_PROPERTY_OPACITY = Shader.PropertyToID(SHADER_PROPERTY_OPACITY);
        ID_SHADER_PROPERTY_SOFTNESS = Shader.PropertyToID(SHADER_PROPERTY_SOFTNESS);
        ID_SHADER_PROPERTY_DOTCOUNT = Shader.PropertyToID(SHADER_PROPERTY_DOTCOUNT);
        ID_SHADER_PROPERTY_DOTSNAP = Shader.PropertyToID(SHADER_PROPERTY_DOTSNAP);
        ID_SHADER_PROPERTY_ZTEST = Shader.PropertyToID(SHADER_PROPERTY_ZTEST);
        ID_SHADER_PROPERTY_WORLD_SIZE = Shader.PropertyToID(SHADER_PROPERTY_WORLD_SIZE);
    }

    private static Material _tracerFigMat;
    public static Material TracerFigMat
    {
        get
        {
            if (_tracerFigMat != null)
                return _tracerFigMat;

            SetupShaderAndCacheProperties();

            _tracerFigMat = new Material(SHADER_UI_FIG);

            _tracerFigMat.DontDestroyAndSetHideFlags();

            _tracerFigMat.SetFloat(ID_SHADER_PROPERTY_OPACITY, 2000f); // EPIC
            _tracerFigMat.SetFloat(ID_SHADER_PROPERTY_SOFTNESS, 1f);
            _tracerFigMat.SetFloat(ID_SHADER_PROPERTY_DOTCOUNT, 100);
            _tracerFigMat.SetFloat(ID_SHADER_PROPERTY_DOTSNAP, 0);
            _tracerFigMat.SetFloat(ID_SHADER_PROPERTY_ZTEST, 4); // LEqual
            _tracerFigMat.SetFloat(ID_SHADER_PROPERTY_WORLD_SIZE, 0.05f);

            Fig.RegisterMaterial(_tracerFigMat);

            return _tracerFigMat;
        }
    }

    private static FigMatWrapper CreateAndRegisterNew()
    {
        SetupShaderAndCacheProperties();

        var figMat = new Material(SHADER_UI_FIG);

        figMat.DontDestroyAndSetHideFlags();

        figMat.SetFloat(ID_SHADER_PROPERTY_OPACITY, 2000f); // EPIC
        figMat.SetFloat(ID_SHADER_PROPERTY_SOFTNESS, 1f);
        figMat.SetFloat(ID_SHADER_PROPERTY_DOTCOUNT, 100);
        figMat.SetFloat(ID_SHADER_PROPERTY_DOTSNAP, 0);
        figMat.SetFloat(ID_SHADER_PROPERTY_ZTEST, 4); // LEqual
        figMat.SetFloat(ID_SHADER_PROPERTY_WORLD_SIZE, 0.05f);

        Fig.RegisterMaterial(figMat);

        return new (figMat);
    }

    private class FigMatWrapper
    {
        public Material Material { get; }
        private bool _inUse;

        public int Frame { get; private set; }
        public bool IsInUse
        {
            get => _inUse;
            set
            {
                _inUse = value;
                
                if (!value)
                    return;

                Frame = Time.frameCount;
            }
        }
        
        public FigMatWrapper(Material material)
        {
            Material = material;
        }
    }
}