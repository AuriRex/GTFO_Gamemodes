using System.Linq;
using GameData;
using Gamemodes.Components;
using Gamemodes.Components.L2;
using Gamemodes.Extensions;
using Il2CppSystem.Collections.Generic;
using Localization;
using Player;
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
    private static GameObject _flashbangThirdpersonPrefab;

    private static GameObject _specialLRFPickupPrefab;

    
    private static ItemDataBlock _flashBlock;
    private static ItemDataBlock _specialLRF;
    
    public static uint SpecialLRF_BlockID { get; private set; }
    public static uint Flashbang_BlockID => _flashBlock?.persistentID ?? 0;

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
    
    public static void PreItemLoading()
    {
        _flashBlock = CloneBlock(ItemDataBlock.GetBlock(SpawnUtils.Consumables.GLOWSTICKS_GREEN));

        _flashBlock.publicName = "Flashbang";
        
        var fbText = new LocalizedText();
        fbText.UntranslatedText = "Flashbang";
        fbText.Id = 0;
        fbText.OldId = 0;
        
        _flashBlock.LocalizedName = fbText;
        
        _flashBlock.ConsumableAmmoMax = 1;
        _flashBlock.ConsumableAmmoMin = 1;

        var og = _flashBlock.FirstPersonPrefabs;
        _flashBlock.FirstPersonPrefabs = new List<string>();
        _flashBlock.FirstPersonPrefabs.Add(og[0]);
        
        _flashBlock.EquipTransitionTime = 0.25f;
        _flashBlock.AimTransitionTime = 0.5f;
        _flashBlock.name = "CONSUMABLE_Flashbang";
        
        ItemDataBlock.AddBlock(_flashBlock);
        
        _specialLRF = CloneBlock(ItemDataBlock.GetBlock(SpawnUtils.Consumables.LONG_RANGE_FLASHLIGHT));

        _specialLRF.publicName = "Personal LRF";
        
        var plrfText = new LocalizedText();
        plrfText.UntranslatedText = "Personal LRF";
        plrfText.Id = 0;
        plrfText.OldId = 0;

        _specialLRF.LocalizedName = plrfText;
        
        _specialLRF.EquipTransitionTime = 0.25f;
        
        _specialLRF.inventorySlot = InventorySlot.ResourcePack;

        _specialLRF.name = $"Personal_{_specialLRF.name}";
        
        ItemDataBlock.AddBlock(_specialLRF);

        SpecialLRF_BlockID = _specialLRF.persistentID;
    }

    
    
    private static void CreatePrefabs()
    {
        // 114 => (Green) Glowstick
        var id = _flashBlock.persistentID;
        
        _flashbangInstancePrefab = UnityEngine.Object.Instantiate(ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][0]);
        _flashbangInstancePrefab.DontDestroyAndSetHideFlags();
        _flashbangInstancePrefab.GetComponent<GlowstickInstance>().SafeDestroy();
        _flashbangInstancePrefab.AddComponent<FlashGrenadeInstance>().enabled = false;
        _flashbangInstancePrefab.name = "Flashbang_Instance";

        _flashbangThirdpersonPrefab =
            UnityEngine.Object.Instantiate(
                ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.ThirdPerson][id][0]);
        _flashbangThirdpersonPrefab.DontDestroyAndSetHideFlags();
        _flashbangThirdpersonPrefab.Children().FirstOrDefault(c => c.name == "Glowstick_1").SafeDestroyGameObject();
        _flashbangThirdpersonPrefab.name = "Flashbang_ThirdPerson";
        
        _pickupBase = UnityEngine.Object.Instantiate(ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id][0]);
        _pickupBase.DontDestroyAndSetHideFlags();
        _pickupBase.Children().FirstOrDefault(c => c.name == "Glow_Stick_Pickup_Lod1").SafeDestroyGameObject();
        _pickupBase.name = "PickupBase_Flashbang";
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.ThirdPerson][id][0] = _flashbangThirdpersonPrefab;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.ThirdPerson][id].Add(_flashbangPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.FirstPerson][id].Add(_flashbangFPPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id][0] = _pickupBase;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id].Add(_flashbangPickupPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][0] = _flashbangInstancePrefab;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][1] = _flashbangPrefab;
        
        Plugin.L.LogWarning($"Added Flashbang prefab! ID:{id}");


        _specialLRFPickupPrefab = UnityEngine.Object.Instantiate(
            ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][SpecialLRF_BlockID][0]);
        _specialLRFPickupPrefab.DontDestroyAndSetHideFlags();
        _specialLRFPickupPrefab.name = "Personal_LRF_Pickup";
        _specialLRFPickupPrefab.transform.localPosition = new Vector3(0, -100, 0);
        var pickupCore = _specialLRFPickupPrefab.GetComponent<ConsumablePickup_Core>();
        SpecialConsumablePickup_Core.TransformOriginal(pickupCore);

        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][SpecialLRF_BlockID][0] =
            _specialLRFPickupPrefab;
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
    
    
    private static ItemDataBlock CloneBlock(ItemDataBlock original)
    {
        var clonedBlock = new ItemDataBlock();

        clonedBlock.publicName = original.publicName;
        clonedBlock.LocalizedName = original.LocalizedName;
        
        clonedBlock.terminalItemShortName = original.terminalItemShortName;
        clonedBlock.terminalItemLongName = original.terminalItemLongName;
        clonedBlock.addSerialNumberToName = original.addSerialNumberToName;
        clonedBlock.registerInTerminalSystem = original.registerInTerminalSystem;
        clonedBlock.DimensionWarpType = original.DimensionWarpType;
        
        clonedBlock.Shard = original.Shard;
        clonedBlock.inventorySlot = original.inventorySlot;
        clonedBlock.FPSSettings = original.FPSSettings;
        
        clonedBlock.crosshair = original.crosshair;
        clonedBlock.HUDIcon = original.HUDIcon;
        clonedBlock.ShowCrosshairWhenAiming = original.ShowCrosshairWhenAiming;
        clonedBlock.GUIShowAmmoClip = original.GUIShowAmmoClip;
        clonedBlock.GUIShowAmmoPack = original.GUIShowAmmoPack;
        clonedBlock.GUIShowAmmoInfinite = original.GUIShowAmmoInfinite;
        clonedBlock.GUIShowAmmoTotalRel = original.GUIShowAmmoTotalRel;
        clonedBlock.canMoveQuick = original.canMoveQuick;
        clonedBlock.ConsumableAmmoMax = original.ConsumableAmmoMax;
        clonedBlock.ConsumableAmmoMin = original.ConsumableAmmoMin;
        clonedBlock.audioEventEquip = original.audioEventEquip;

        clonedBlock.FirstPersonPrefabs = original.FirstPersonPrefabs;
        
        clonedBlock.ThirdPersonPrefabs = original.ThirdPersonPrefabs;
        
        clonedBlock.PickupPrefabs = original.PickupPrefabs;
        
        clonedBlock.InstancePrefabs = original.InstancePrefabs;
        
        clonedBlock.EquipTransitionTime = original.EquipTransitionTime;
        clonedBlock.AimTransitionTime = original.AimTransitionTime;
        clonedBlock.LeftHandGripAlign = original.LeftHandGripAlign;
        clonedBlock.LeftHandGripAnim = original.LeftHandGripAnim;
        clonedBlock.RightHandGripAlign = original.RightHandGripAlign;
        clonedBlock.RightHandGripAnim = original.RightHandGripAnim;
        clonedBlock.name = original.name;
        clonedBlock.internalEnabled = true;

        return clonedBlock;
    }
}