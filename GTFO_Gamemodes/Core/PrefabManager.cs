using System.Linq;
using GameData;
using Gamemodes.Components.L2;
using Gamemodes.Extensions;
using Il2CppSystem.Collections.Generic;
using Localization;
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

    private static ItemDataBlock _flashBlock;
    
    public static void PreItemLoading()
    {
        var glowsticks = ItemDataBlock.GetBlock(114);

        _flashBlock = new ItemDataBlock();

        _flashBlock.publicName = "Flashbang";
        _flashBlock.LocalizedName = new LocalizedText();
        
        _flashBlock.LocalizedName.UntranslatedText = "Flashbang";
        
        _flashBlock.terminalItemShortName = glowsticks.terminalItemShortName;
        _flashBlock.terminalItemLongName = glowsticks.terminalItemLongName;
        _flashBlock.addSerialNumberToName = glowsticks.addSerialNumberToName;
        _flashBlock.registerInTerminalSystem = glowsticks.registerInTerminalSystem;
        _flashBlock.DimensionWarpType = glowsticks.DimensionWarpType;
        
        _flashBlock.Shard = glowsticks.Shard;
        _flashBlock.inventorySlot = glowsticks.inventorySlot;
        _flashBlock.FPSSettings = glowsticks.FPSSettings;
        
        _flashBlock.crosshair = glowsticks.crosshair;
        _flashBlock.HUDIcon = glowsticks.HUDIcon;
        _flashBlock.ShowCrosshairWhenAiming = glowsticks.ShowCrosshairWhenAiming;
        _flashBlock.GUIShowAmmoClip = glowsticks.GUIShowAmmoClip;
        _flashBlock.GUIShowAmmoPack = glowsticks.GUIShowAmmoPack;
        _flashBlock.GUIShowAmmoInfinite = glowsticks.GUIShowAmmoInfinite;
        _flashBlock.GUIShowAmmoTotalRel = glowsticks.GUIShowAmmoTotalRel;
        _flashBlock.canMoveQuick = glowsticks.canMoveQuick;
        _flashBlock.ConsumableAmmoMax = 1;
        _flashBlock.ConsumableAmmoMin = 1;
        _flashBlock.audioEventEquip = glowsticks.audioEventEquip;
        
        _flashBlock.FirstPersonPrefabs = new List<string>();
        _flashBlock.FirstPersonPrefabs.Add(glowsticks.FirstPersonPrefabs[0]);
        
        _flashBlock.ThirdPersonPrefabs = glowsticks.ThirdPersonPrefabs;
        
        _flashBlock.PickupPrefabs = glowsticks.PickupPrefabs;
        
        _flashBlock.InstancePrefabs = glowsticks.InstancePrefabs;
        
        _flashBlock.EquipTransitionTime = 0.25f;
        _flashBlock.AimTransitionTime = 0.5f;
        _flashBlock.LeftHandGripAlign = glowsticks.LeftHandGripAlign;
        _flashBlock.LeftHandGripAnim = glowsticks.LeftHandGripAnim;
        _flashBlock.RightHandGripAlign = glowsticks.RightHandGripAlign;
        _flashBlock.RightHandGripAnim = glowsticks.RightHandGripAnim;
        _flashBlock.name = "CONSUMABLE_Flashbang";
        _flashBlock.internalEnabled = true;
        
        ItemDataBlock.AddBlock(_flashBlock);
    }
    
    private static void CreatePrefabs()
    {
        // 114 => (Green) Glowstick
        var id = _flashBlock.persistentID;
        
        _flashbangInstancePrefab = UnityEngine.Object.Instantiate(ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][0]);
        _flashbangInstancePrefab.DontDestroyAndSetHideFlags();
        _flashbangInstancePrefab.GetComponent<GlowstickInstance>().SafeDestroy();
        _flashbangInstancePrefab.AddComponent<FlashGrenadeInstance>().enabled = false;

        _pickupBase = UnityEngine.Object.Instantiate(ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id][0]);
        _pickupBase.DontDestroyAndSetHideFlags();
        _pickupBase.Children().FirstOrDefault(c => c.name == "Glow_Stick_Pickup_Lod1").SafeDestroyGameObject();

        // TODO: Third-Person prefab
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.FirstPerson][id].Add(_flashbangFPPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id][0] = _pickupBase;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id].Add(_flashbangPickupPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][0] = _flashbangInstancePrefab;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][1] = _flashbangPrefab;
        
        Plugin.L.LogWarning($"Added Flashbang prefab! ID:{id}");
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