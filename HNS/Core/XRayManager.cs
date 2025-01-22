using System.Linq;
using Gamemodes.Extensions;
using HNS.Components;
using HNS.Net;
using HNS.Net.Packets;
using UnityEngine;

namespace HNS.Core;

public static class XRayManager
{
    private const int INSTANCE_COUNT = 8;
    private static XRayInstance[] _instances;

    private static Material _material;
    public static Material XRayMaterial
    {
        get
        {
            if (_material == null)
            {
                _material = new Material(Shader.Find("FX/XRayParticle"));
                _material.SetColor("_Color", Color.white);
            }

            return _material;
        }
    }
    
    internal static void Init()
    {
        if (_instances != null)
            return;
        
        _instances = new XRayInstance[INSTANCE_COUNT];

        for (int i = 0; i < INSTANCE_COUNT; i++)
        {
            _instances[i] = CreateInstance(i);
        }
    }

    public static void SendCastXRays(Vector3 origin, Vector3 direction)
    {
        origin += direction.normalized * 0.33f;
        
        NetSessionManager.SendXRayAction(origin, direction);
    }

    public static void OnXRayDataReceived(pXRayAction data)
    {
        if (!CastXRays(data.Position, data.Direction))
            Plugin.L.LogError($"{nameof(OnXRayDataReceived)}: received data but no instances are available.");
    }
    
    private static bool CastXRays(Vector3 origin, Vector3 direction)
    {
        var instance = _instances.FirstOrDefault(i => i.IsAvailable);

        if (instance == null)
            return false;

        instance.StartCasting(origin, direction);

        return true;
    }
    
    private static XRayInstance CreateInstance(int id)
    {
        var holder = new GameObject($"XRayInstance_{id}");

        holder.DontDestroyAndSetHideFlags();
        
        return holder.AddComponent<XRayInstance>();
    }
    
}