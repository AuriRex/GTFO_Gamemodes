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

    private static Shader _shader;

    private static AssetBundle _flashbangBundle;
    
    private static GameObject _flashbangPrefab;
    private static GameObject _flashbangFPPrefab;
    private static GameObject _flashbangPickupPrefab;

    private static GameObject _smokenadePrefab;
    private static GameObject _smokenadePickupPrefab;
    private static GameObject _smokenadeFPPrefab;
    
    private static GameObject _specialLRFPickupPrefab;

    
    private static ItemDataBlock _flashBlock;
    private static ItemDataBlock _smokeBlock;
    private static ItemDataBlock _specialLRF;
    
    public static uint SpecialLRF_BlockID { get; private set; }
    public static uint Flashbang_BlockID => _flashBlock?.persistentID ?? 0;
    public static uint Smokenade_BlockID => _smokeBlock?.persistentID ?? 0;

    internal static void Init()
    {
        _flashbangBundle = AssetBundle.LoadFromMemory(Resources.Data.flashbundle);
        
        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangmodel.prefab", out _flashbangPrefab);
        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangpickup.prefab", out _flashbangPickupPrefab);
        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangfirstperson.prefab", out _flashbangFPPrefab);

        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangmodel.prefab", out _smokenadePrefab);
        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangpickup.prefab", out _smokenadePickupPrefab);
        LoadPrefab(_flashbangBundle, "assets/stunnade/flashbangfirstperson.prefab", out _smokenadeFPPrefab);

        _smokenadePrefab = UnityEngine.Object.Instantiate(_flashbangPrefab);
        _smokenadePrefab.DontDestroyAndSetHideFlags();
        
        _smokenadePickupPrefab = UnityEngine.Object.Instantiate(_flashbangPickupPrefab);
        _smokenadePickupPrefab.DontDestroyAndSetHideFlags();
        
        _smokenadeFPPrefab = UnityEngine.Object.Instantiate(_flashbangFPPrefab);
        _smokenadeFPPrefab.DontDestroyAndSetHideFlags();

        var material = new Material(_flashbangPrefab.GetComponentsInChildren<Renderer>()[0].sharedMaterial);
        material.DontDestroyAndSetHideFlags();
        var color = new Color(0.1f, 0.1f, 0.2f, 1);
        material.color = color;
        ReplaceSharedMaterialOnAllRenderers(_smokenadePrefab, material);
        ReplaceSharedMaterialOnAllRenderers(_smokenadePickupPrefab, material);
        ReplaceSharedMaterialOnAllRenderers(_smokenadeFPPrefab, material);
        
        _flashbangBundle.Unload(false);
    }

    private static void ReplaceSharedMaterialOnAllRenderers(GameObject go, Material material) 
    {
        foreach (var renderer in go.GetComponentsInChildren<Renderer>(true))
        {
            renderer.sharedMaterial = material;
        }
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
        _flashBlock = CreateGrenadeDataBlock("Flashbang");

        ItemDataBlock.AddBlock(_flashBlock);
        
        _smokeBlock = CreateGrenadeDataBlock("Smoke Grenade");
        ItemDataBlock.AddBlock(_smokeBlock);
        
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

    private static ItemDataBlock CreateGrenadeDataBlock(string name, int consumableMin = 1, int consumableMax = 1)
    {
        var itemDB = CloneBlock(ItemDataBlock.GetBlock(SpawnUtils.Consumables.GLOWSTICKS_GREEN));

        itemDB.publicName = name;
        
        var fbText = new LocalizedText();
        fbText.UntranslatedText = name;
        fbText.Id = 0;
        fbText.OldId = 0;
        
        itemDB.LocalizedName = fbText;
        
        itemDB.ConsumableAmmoMax = consumableMax;
        itemDB.ConsumableAmmoMin = consumableMin;

        var og = itemDB.FirstPersonPrefabs;
        itemDB.FirstPersonPrefabs = new List<string>();
        itemDB.FirstPersonPrefabs.Add(og[0]);
        
        itemDB.EquipTransitionTime = 0.25f;
        itemDB.AimTransitionTime = 0.5f;
        itemDB.name = $"CONSUMABLE_{name.Replace(' ', '_')}";

        return itemDB;
    }
    
    private static void CreatePrefabs()
    {
        // 114 => (Green) Glowstick
        CreateGrenadePrefabs<FlashGrenadeInstance>(_flashBlock, _flashbangPrefab, _flashbangFPPrefab, _flashbangPickupPrefab);

        CreateGrenadePrefabs<SmokeGrenadeInstance>(_smokeBlock, _smokenadePrefab, _smokenadeFPPrefab, _smokenadePickupPrefab);

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

    private static void CreateGrenadePrefabs<TInstance>(ItemDataBlock itemDB, GameObject thirdPersonPrefab, GameObject firstPersonPrefab, GameObject pickupPrefab) where TInstance : GenericGrenadeInstance
    {
        var id = itemDB.persistentID;
        var name = itemDB.name;
        
        var grenadeInstancePrefab = UnityEngine.Object.Instantiate(ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][0]);
        grenadeInstancePrefab.DontDestroyAndSetHideFlags();
        grenadeInstancePrefab.GetComponent<GlowstickInstance>().SafeDestroy();
        grenadeInstancePrefab.AddComponent<TInstance>().enabled = false;
        grenadeInstancePrefab.name = name + "_Instance";

        var grenadeThirdpersonPrefab =
            UnityEngine.Object.Instantiate(
                ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.ThirdPerson][id][0]);
        grenadeThirdpersonPrefab.DontDestroyAndSetHideFlags();
        grenadeThirdpersonPrefab.Children().FirstOrDefault(c => c.name == "Glowstick_1").SafeDestroyGameObject();
        grenadeThirdpersonPrefab.name = name + "_ThirdPerson";
        
        var pickupBase = UnityEngine.Object.Instantiate(ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id][0]);
        pickupBase.DontDestroyAndSetHideFlags();
        pickupBase.Children().FirstOrDefault(c => c.name == "Glow_Stick_Pickup_Lod1").SafeDestroyGameObject();
        pickupBase.name = "PickupBase_" + name;
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.ThirdPerson][id][0] = grenadeThirdpersonPrefab;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.ThirdPerson][id].Add(thirdPersonPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.FirstPerson][id].Add(firstPersonPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id][0] = pickupBase;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Pickup][id].Add(pickupPrefab);
        
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][0] = grenadeInstancePrefab;
        ItemSpawnManager.m_loadedPrefabsPerItemMode[(int)ItemMode.Instance][id][1] = thirdPersonPrefab;
        
        Plugin.L.LogWarning($"Added {name} prefab! ID:{id}");
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