using System.Linq;
using Gamemodes.Components.L2;
using Gamemodes.Extensions;
using UnityEngine;
using Object = System.Object;

namespace Gamemodes.Core;

public static class PrefabManager
{
    private static GameObject _pickupBase;
    private static Shader _shader;

    private static AssetBundle _flashbangBundle;
    private static GameObject _flashbangPrefab;
    private static GameObject _flashbangFPPrefab;
    private static GameObject _flashbangPickupPrefab;
    private static GameObject _flashbangInstancePrefab;
    
    internal static void Init()
    {
        _flashbangBundle = AssetBundle.LoadFromMemory(Resources.Data.flashbundle);
        
        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangmodel.prefab", out _flashbangPrefab);
        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangpickup.prefab", out _flashbangPickupPrefab);
        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangfirstperson.prefab", out _flashbangFPPrefab);

        _flashbangBundle.Unload(false);
    }

    private static void LoadPrefab(AssetBundle bundle, string asset, out GameObject go)
    {
        go = bundle.LoadAsset(asset).Cast<GameObject>();
        
        go.DontDestroyAndSetHideFlags();

        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            ReplaceMatShader(renderer.sharedMaterial);
        }
    }

    internal static void OnAssetLoaded()
    {
        CreatePrefabs();
    }

    private static void CreatePrefabs()
    {
        // 114 => (Green) Glowstick
        
        _flashbangInstancePrefab = UnityEngine.Object.Instantiate(ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][114][0]);
        _flashbangInstancePrefab.DontDestroyAndSetHideFlags();
        _flashbangInstancePrefab.GetComponent<GlowstickInstance>().SafeDestroy();
        _flashbangInstancePrefab.AddComponent<FlashGrenadeInstance>().enabled = false;

        _pickupBase = UnityEngine.Object.Instantiate(ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][114][0]);
        _pickupBase.DontDestroyAndSetHideFlags();
        _pickupBase.Children().FirstOrDefault(c => c.name == "Glow_Stick_Pickup_Lod1").SafeDestroyGameObject();

        // TODO: Third-Person prefab
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.FirstPerson][114][1] = _flashbangFPPrefab;
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][114][0] = _pickupBase;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][114].Add(_flashbangPickupPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][114][0] = _flashbangInstancePrefab;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][114][1] = _flashbangPrefab;
    }

    private static void ReplaceMatShader(Material mat)
    {
        if (_shader == null)
        {
            _shader = Shader.Find("GTFO/Standard");
        }

        mat.shader = _shader;
        
        mat.SetFloat("_EnableFPSRendering", 1);
        mat.EnableKeyword("ENABLE_FPS_RENDERING");
    }
}