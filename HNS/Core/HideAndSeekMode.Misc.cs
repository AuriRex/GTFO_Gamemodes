using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using GameData;
using Gamemodes.Core;
using Gamemodes.Net;
using Gear;
using HNS.Components;
using HNS.Net;
using Player;
using SNetwork;
using UnityEngine;

namespace HNS.Core;

internal partial class HideAndSeekMode
{
    private static Coroutine _flashSpawnerRoutine;
    
    private static void SetupGearSelectors()
    {
        if (_gearMeleeSelector != null)
        {
            return;
        }
        
        var gear = GearManager.GetAllGearForSlot(InventorySlot.GearMelee).ToArray();

        _gearMeleeSelector = new(gear, InventorySlot.GearMelee);
        _gearMeleeSelector.RefillGunsAndToolOnPick = () => false;
        
        var classGear = GearManager.GetAllGearForSlot(InventorySlot.GearClass).ToArray();

        _gearHiderSelector = new (classGear.Where(g => !g.PublicGearName?.Contains("Sentry") ?? false), InventorySlot.GearClass);
        _gearHiderSelector.RefillGunsAndToolOnPick = DoRefillGunsAndToolOnPick;
        _gearHiderSelector.OnPickedGear += OnPickedGear;
        
        var seekerStuffs = new string[]
        {
            "Krieger",
            //"Stalwart Flow",
            "Optron"
        };

        var seekerGear = classGear.Where(g =>
            (g.PublicGearName.Contains("Sentry") /*&& !g.PublicGearName.Contains("Sniper")*/)
            || seekerStuffs.Any(s => g.PublicGearName.Contains(s)));
        
        _gearSeekerSelector = new (seekerGear, InventorySlot.GearClass);
        _gearSeekerSelector.RefillGunsAndToolOnPick = DoRefillGunsAndToolOnPick;
        _gearSeekerSelector.OnPickedGear += OnPickedGear;
    }

    private static void OnPickedGear(GearIDRange gear, InventorySlot slot)
    {
        if (NetSessionManager.HasSession)
        {
            _pickToolCooldownEnd = DateTimeOffset.UtcNow.AddSeconds(TOOL_SELECT_COOLDOWN);
        }

        if (slot == InventorySlot.GearClass)
        {
            SetToolAmmoForLocalPlayer();
        }
    }
    
    internal static void SetToolAmmoForLocalPlayer()
    {
        var value = 1f;
        var team = (GMTeam)NetworkingManager.GetLocalPlayerInfo().Team;

        if (TeamHelper.IsHider(team))
        {
            value = 0.125f;
        }
        else if (TeamHelper.IsSeeker(team))
        {
            value = 0.125f * 3;
        }

        GearUtils.LocalReserveAmmoAction(GearUtils.AmmoType.Tool, GearUtils.AmmoAction.SetToPercent, value);
    }

    private static bool DoRefillGunsAndToolOnPick()
    {
        return !NetSessionManager.HasSession;
    }

    private static string HiderExtraInfoUpdater(PlayerWrapper player)
    {
        if (player.IsLocal)
            return GetZoneAndAreaInfo(player, "color=green");

        if (!player.CanBeSeenByLocalPlayer())
            return null;

        return GetZoneAndAreaInfo(player);
    }

    private static string SeekersExtraInfoUpdater(PlayerWrapper player)
    {
        if (!player.CanBeSeenByLocalPlayer())
            return null;

        if (NetSessionManager.HasSession && !NetSessionManager.CurrentSession.SetupTimeFinished)
        {
            return "<color=white>[<color=red> ? ? ? </color>]</color>";
        }

        if (!player.IsLocal)
            return GetZoneAndAreaInfo(player);

        return GetZoneAndAreaInfo(player, "color=green");
    }

    private static string GetZoneAndAreaInfo(PlayerWrapper player, string color = "color=white")
    {
        var area = player.PlayerAgent.CourseNode?.m_area;

        if (area == null)
            return null;

        return
            $"<{color}>[<color=orange>ZONE {area.m_zone.NavInfo.Number}</color>, <color=orange>Area {area.m_navInfo.Suffix}</color>]";
    }

    internal static void DespawnMineInstancesAndCFoamBlobs()
    {
        var mineInstances = ToolInstanceCaches.MineCache.All;
        var cfoam = ToolInstanceCaches.GlueCache.All;

        CoroutineManager.StartCoroutine(DespawnMineInstancesAndCFoamBlobsCoroutine(cfoam, mineInstances).WrapToIl2Cpp());
    }

    private static IEnumerator DespawnMineInstancesAndCFoamBlobsCoroutine(IEnumerable<GlueGunProjectile> cfoamBlobs, IEnumerable<MineDeployerInstance> mines)
    {
        yield return null;

        int count = 0;
        foreach (var blob in cfoamBlobs)
        {
            ProjectileManager.WantToDestroyGlue(blob.SyncID);

            count++;
            if (DoYield(ref count))
                yield return null;
        }

        foreach (var mine in mines)
        {
            if (mine == null || mine.Replicator == null || mine.Replicator.WasCollected)
                continue;

            ItemReplicationManager.DeSpawn(mine.Replicator);

            count++;
            if (DoYield(ref count))
                yield return null;
        }

        yield break;

        bool DoYield(ref int count)
        {
            const int YIELD_COUNT = 25;

            if (count < YIELD_COUNT)
            {
                return false;
            }

            count = 0;
            return true;
        }
    }

    private static void HideResourcePacks()
    {
        _originalResourcePackLocations.Clear();

        foreach (var rpp in UnityEngine.Object.FindObjectsOfType<ResourcePackPickup>())
        {
            var pos = rpp.transform.position;

            _originalResourcePackLocations.Add(new()
            {
                pickup = rpp,
                position = pos,
            });

            rpp.gameObject.SetActive(false);
        }

        if (!SNet.IsMaster)
        {
            return;
        }

        //StartFlashSpawnerRoutine();
    }

    internal static void StartFlashSpawnerRoutine()
    {
        StopFlashSpawnerRoutine();
        _flashSpawnerRoutine = CoroutineManager.StartCoroutine(SpawnFlashbangs().WrapToIl2Cpp());
    }

    private static void StopFlashSpawnerRoutine()
    {
        if (_flashSpawnerRoutine == null)
            return;

        CoroutineManager.StopCoroutine(_flashSpawnerRoutine);
        _flashSpawnerRoutine = null;
    }

    private static IEnumerator SpawnFlashbangs()
    {
        if (!SNet.IsMaster)
            yield break;

        var stopWatch = Stopwatch.StartNew();
        Plugin.L.LogDebug("Spawning Flashbangs ...");
        var pickups = UnityEngine.Object.FindObjectsOfType<ConsumablePickup_Core>();

        var flashbangs = pickups.Where(p => p.name.Contains("Flashbang", StringComparison.InvariantCultureIgnoreCase))
            .ToArray();

        var spawnCount = 0;
        foreach (var info in _originalResourcePackLocations)
        {
            info.LateSetup();

            bool slotOccupied = pickups.Any(fb => Vector3.Distance(fb.transform.position, info.position) < 0.05f);

            if (slotOccupied)
                continue;

            NetworkingManager.SendSpawnItemInLevel(info.node, info.position, PrefabManager.Flashbang_BlockID);
            spawnCount++;
            yield return null;
        }

        stopWatch.Stop();
        Plugin.L.LogDebug($"Spawning Flashbangs complete! Took {stopWatch.ElapsedMilliseconds} ms total, spawned in {spawnCount} new ones.");
    }

    private static ClothesPalette GetLobbyPalette(GMTeam team)
    {
        return team switch
        {
            GMTeam.PreGameAlpha => _palette_teamAlpha,
            GMTeam.PreGameBeta => _palette_teamBeta,
            GMTeam.PreGameGamma => _palette_teamGamma,
            GMTeam.PreGameDelta => _palette_teamDelta,
            _ => _spectatorPalette,
        };
    }

    private static void SetHelmetLights(PlayerSyncModelData syncModel, float intensity = 0.8f, float range = 0.06f, Color? color = null)
    {
        color ??= HELMET_LIGHT_DEFAULT_COLOR;

        foreach (var kvp in syncModel.m_helmetLights)
        {
            var light = kvp.Key;
            if (light == null)
                continue;

            if (light.name.Contains("Flashlight"))
            {
                // Unity light has to be off, else we get lights on if it shouldn't be
                light.enabled = false;
                continue;
            }

            light.color = color.Value;
            light.intensity = intensity;
            light.range = range;
        }
    }

    private static void StoreOriginalAndAssignCustomPalette(PlayerWrapper info, PaletteStorage storage, ClothesPalette paletteToSet)
    {
        if (storage.hiderPalette == null)
        {
            storage.hiderPalette = info.PlayerAgent.RigSwitch.m_currentPalette;
        }

        if (paletteToSet == null)
        {
            paletteToSet = _seekerPalette;
        }

        info.PlayerAgent.RigSwitch.ApplyPalette(paletteToSet);
    }

    private static void RevertToOriginalPalette(PlayerWrapper info, PaletteStorage storage)
    {
        if (storage.hiderPalette == null)
            return;

        info.PlayerAgent.RigSwitch.ApplyPalette(storage.hiderPalette);
        storage.hiderPalette = null;
    }

    public static void SetDisplayedLocalPlayerHealth(float healthRel)
    {
        var playerStatus = GuiManager.PlayerLayer.m_playerStatus;

        playerStatus.m_lastHealthVal = healthRel;
        playerStatus.UpdateHealth(healthRel);
    }
    
    public static void SetNearDeathAudioLimit(LocalPlayerAgent player, bool enable)
    {
        // Not even sure if this works lol
        var localDamage = player.Damage.Cast<Dam_PlayerDamageLocal>();

        //player.Breathing.enabled = enable;

        if (enable)
        {
            if (ORIGINAL_m_nearDeathAudioLimit != -1)
            {
                localDamage.m_nearDeathAudioLimit = ORIGINAL_m_nearDeathAudioLimit;
            }

            return;
        }

        if (ORIGINAL_m_nearDeathAudioLimit == -1)
        {
            ORIGINAL_m_nearDeathAudioLimit = localDamage.m_nearDeathAudioLimit;
        }

        localDamage.m_nearDeathAudioLimit = -1f;
    }

    public static void SetLocalPlayerStatusUIElementsActive(bool isSeeker)
    {
        var status = GuiManager.PlayerLayer.m_playerStatus;

        status.m_warning.gameObject.SetActive(!isSeeker);

        if (isSeeker)
        {
            status.UpdateShield(1f);
            status.m_shieldText.SetText("Seeker");
        }

        status.m_shieldUIParent.gameObject.SetActive(isSeeker);

        for (int i = 0; i < status.transform.childCount; i++)
        {
            var child = status.transform.GetChild(i).gameObject;

            if (child.name != "HealthBar")
                continue;

            child.SetActive(!isSeeker);
        }
    }

    private static void RemoveAngySentries()
    {
        var allBlocks = PlayerOfflineGearDataBlock.Wrapper.Blocks.ToArray();

        foreach (var block in allBlocks)
        {
            if (block.name.StartsWith("SentryGun"))
            {
                block.internalEnabled = true;
                continue;
            }

            if (!block.name.StartsWith(PREFIX_ANGY_SENTRY))
                continue;

            PlayerOfflineGearDataBlock.RemoveBlockByID(block.persistentID);
            PlayerOfflineGearDataBlock.s_blockIDByName.Remove(block.name);
            PlayerOfflineGearDataBlock.s_blockByID.Remove(block.persistentID);
            PlayerOfflineGearDataBlock.s_dirtyBlocks.Remove(block.persistentID);
        }

        RefreshGear();

        PlayerBackpackManager.EquipLocalGear(GearManager.Current.m_gearPerSlot[(int)InventorySlot.GearClass].ToArray()[0]);
    }

    private static void AddAngySentries()
    {
        var allBlocks = PlayerOfflineGearDataBlock.Wrapper.Blocks.ToArray();

        bool refreshGear = false;

        foreach (var block in allBlocks)
        {
            if (!block.name.StartsWith("SentryGun"))
                continue;

            var angyName = $"{PREFIX_ANGY_SENTRY}{block.name}";

            if (allBlocks.Any(b => b.name == angyName))
                continue;

            if (!block.name.Contains("Sniper"))
            {
                SetupAngySentry(block, angyName);
                refreshGear = true;
            }

            block.internalEnabled = false;
        }

        if (refreshGear)
        {
            RefreshGear();
        }
    }

    private static void RefreshGear()
    {
        GearManager manager = GearManager.Current;

        int length = Enum.GetValues(typeof(InventorySlot)).Length;
        manager.m_gearPerSlot = new Il2CppSystem.Collections.Generic.List<GearIDRange>[length];
        for (int j = 0; j < length; j++)
        {
            manager.m_gearPerSlot[j] = new();
        }

        manager.LoadOfflineGearDatas();
        GearManager.GenerateAllGearIcons();
    }

    private static void SetupAngySentry(PlayerOfflineGearDataBlock blockOriginal, string angyName)
    {
        var block = new PlayerOfflineGearDataBlock();

        block.persistentID = 0;
        block.name = angyName;
        block.Type = blockOriginal.Type;
        block.internalEnabled = true;

        Plugin.L.LogDebug($"Setting up {angyName} ...");

        var gearIDRange = new GearIDRange(blockOriginal.GearJSON);

        gearIDRange.SetCompID(eGearComponent.ToolTargetingType, (int)eSentryGunDetectionType.EnemiesAndPlayers);
        gearIDRange.SetCompID(eGearComponent.ToolTargetingPart, 4); // idk lol

        var fireMode = (eWeaponFireMode)gearIDRange.GetCompID(eGearComponent.FireMode);

        string displayName = "???";

        switch (fireMode)
        {
            default:
                break;
            case eWeaponFireMode.SentryGunBurst:
                displayName = "Burst";
                break;
            case eWeaponFireMode.SentryGunSemi:
                displayName = "Sniper";
                break;
            case eWeaponFireMode.SentryGunAuto:
                displayName = "Auto";
                break;
            case eWeaponFireMode.SentryGunShotgunSemi:
                displayName = "Shotgun";
                break;
        }

        displayName = $"<#c00>Angry {displayName} Sentry</color>";

        gearIDRange.PublicGearName = displayName;
        gearIDRange.PlayfabItemName = displayName;

        block.GearJSON = gearIDRange.ToJSON();

        PlayerOfflineGearDataBlock.AddBlock(block);
    }
    
    private static void CreatePalette(string name, Color color, int material, out ClothesPalette palette)
    {
        var go = new GameObject(name);

        go.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        UnityEngine.Object.DontDestroyOnLoad(go);

        palette = go.AddComponent<ClothesPalette>();

        var tone = new ClothesPalette.Tone()
        {
            m_color = color,
            m_materialOverride = material,
            m_texture = Texture2D.whiteTexture,
        };

        palette.m_textureTiling = 1;
        palette.m_primaryTone = tone;
        palette.m_secondaryTone = tone;
        palette.m_tertiaryTone = tone;
        palette.m_quaternaryTone = tone;
        palette.m_quinaryTone = tone;
    }

    private static void WarpAllPlayersToMaster()
    {
        var localPlayer = PlayerManager.GetLocalPlayerAgent();

        foreach (var player in NetworkingManager.AllValidPlayers)
        {
            if (player.IsLocal)
                continue;

            if (player.Team == (int)GMTeam.Camera)
                continue;

            player.WarpTo(localPlayer.Position, localPlayer.TargetLookDir, localPlayer.DimensionIndex, PlayerAgent.WarpOptions.PlaySounds | PlayerAgent.WarpOptions.ShowScreenEffectForLocal | PlayerAgent.WarpOptions.WithoutBots);
        }
    }
}