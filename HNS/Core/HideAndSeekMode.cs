using GameData;
using Gamemodes;
using Gamemodes.Extensions;
using Gamemodes.Mode;
using Gamemodes.Net;
using Gear;
using HarmonyLib;
using HNS.Components;
using HNS.Net;
using HNS.Patches;
using Il2CppInterop.Runtime.Injection;
using Player;
using SNetwork;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Components;
using UnityEngine;

namespace HNS.Core;

internal partial class HideAndSeekMode : GamemodeBase
{
    public static HideAndSeekGameManager GameManager { get; private set; }

    public override string ID => "hideandseek";

    public override string DisplayName => "Hide and Seek";

    public override string Description => "No Enemies\nAll Doors Open\n\n<#f00>Seekers</color> have to catch all <#0ff>Hiders</color>";

    public override ModeSettings Settings => new ModeSettings
    {
        AllowMidGameModeSwitch = false,
        PreventDefaultFailState = true,
        PreventExpeditionEnemiesSpawning = true,
        PreventPlayerRevives = true,
        PreventRespawnRoomsRespawning = true,
        BlockWorldEvents = true,
        OpenAllSecurityDoors = true,
        OpenAllWeakDoors = true,
        RemoveCheckpoints = true,
        AllowForcedTeleportation = true,
        RevealEntireMap = true,
        MapIconsToReveal = Utils.EVERYTHING_EXCEPT_LOCKERS,
        ForceAddArenaDimension = true,
        DisableVoiceLines = true,
        UseTeamVisibility = true,
        RemoveBloodDoors = true,
        RemoveTerminalCommands = true,
        InfiniteBackpackAmmo = true,
        InfiniteSentryAmmo = true,
        PushForceMultiplier = 2.5f,
    };

    private Harmony _harmonyInstance;

    private GameObject _gameManagerGO;

    private static int DEFAULT_MASK_MELEE_ATTACK_TARGETS;
    private static int DEFAULT_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

    private static int DEFAULT_MASK_BULLETWEAPON_RAY;
    private static int DEFAULT_MASK_BULLETWEAPON_PIERCING_PASS;


    private static int MODIFIED_MASK_MELEE_ATTACK_TARGETS;
    private static int MODIFIED_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

    private static int MODIFIED_MASK_BULLETWEAPON_RAY;
    private static int MODIFIED_MASK_BULLETWEAPON_PIERCING_PASS;
    
    private static ClothesPalette _seekerPalette;
    private static ClothesPalette _spectatorPalette;
    
    private static float ORIGINAL_m_nearDeathAudioLimit = -1;

    private static float SEEKER_LIGHT_INTENSITY = 5;
    private static float SEEKER_LIGHT_RANGE = 0.15f;
    private static Color SEEKER_LIGHT_COLOR = Color.red;
    
    public override void Init()
    {
        _harmonyInstance = new Harmony(Plugin.GUID);
        NetSessionManager.Init();

        ChatCommands.Add("hnsstart", StartGame)
            .Add("hnsstop", StopGame)
            .Add("seeker", SwitchToSeeker)
            .Add("hider", SwitchToHider)
            .Add("lobby", SwitchToLobby);

        CreateSeekerPalette();

        TeamVisibility.Team((int)GMTeam.Seekers).CanSeeSelf();

        TeamVisibility.Team((int)GMTeam.PreGameAndOrSpectator).CanSeeSelf().And((int)GMTeam.Seekers, (int)GMTeam.Hiders);

        _gameManagerGO = new GameObject("HideAndSeek_Manager");

        UnityEngine.Object.DontDestroyOnLoad(_gameManagerGO);
        _gameManagerGO.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;

        _gameManagerGO.SetActive(false);

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<PaletteStorage>())
        {
            ClassInjector.RegisterTypeInIl2Cpp<PaletteStorage>();
        }

        DEFAULT_MASK_MELEE_ATTACK_TARGETS = LayerManager.MASK_MELEE_ATTACK_TARGETS;
        MODIFIED_MASK_MELEE_ATTACK_TARGETS = LayerManager.Current.GetMask(new string[]
        {
            "EnemyDamagable",
            "Dynamic",
            "PlayerSynced" // <-- Added
        });

        DEFAULT_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;
        MODIFIED_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = LayerManager.Current.GetMask(new string[]
        {
            "EnemyDamagable",
            "Dynamic",
            "Default",
            "Default_NoGraph",
            "Default_BlockGraph",
            "EnemyDead",
            "PlayerSynced" // <-- Added
        });

        DEFAULT_MASK_BULLETWEAPON_RAY = LayerManager.MASK_BULLETWEAPON_RAY;
        MODIFIED_MASK_BULLETWEAPON_RAY = LayerManager.Current.GetMask(new string[]
        {
            "Default",
            "Default_NoGraph",
            "Default_BlockGraph",
            "EnemyDamagable",
            "ProjectileBlocker",
            "Dynamic",
            //"PlayerSynced" // <-- Removed
        });

        DEFAULT_MASK_BULLETWEAPON_PIERCING_PASS = LayerManager.MASK_BULLETWEAPON_PIERCING_PASS;
        MODIFIED_MASK_BULLETWEAPON_PIERCING_PASS = LayerManager.Current.GetMask(new string[]
        {
            "EnemyDamagable",
            //"PlayerSynced" // <-- Removed
        });

        GameManager = new HideAndSeekGameManager(_gameManagerGO.AddComponent<TimerHUD>());
    }

    private static void CreateSeekerPalette()
    {
        CreatePalette("SeekerPalette", Color.red, 6, out _seekerPalette);
        CreatePalette("SpectatorPalette", Color.cyan, 9, out _spectatorPalette);
    }

    private static void CreatePalette(string name, Color color, int material, out ClothesPalette palette)
    {
        var go = new GameObject(name);

        go.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        GameObject.DontDestroyOnLoad(go);

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

            player.WarpTo(localPlayer.Position, localPlayer.TargetLookDir, localPlayer.DimensionIndex, PlayerAgent.WarpOptions.PlaySounds | PlayerAgent.WarpOptions.ShowScreenEffectForLocal | PlayerAgent.WarpOptions.WithoutBots);
        }
    }

    private static IEnumerator LightBringer()
    {
        var yielder = new WaitForSeconds(5f);
        yield return yielder;
        Plugin.L.LogDebug("There shall be light! (LRFs are being distributed.)");
        GiveAllPlayersLights();
    }
    
    private static void GiveAllPlayersLights()
    {
        foreach (var player in PlayerManager.PlayerAgentsInLevel)
        {
            if (player.Owner.IsBot)
                continue;
            
            NetworkingManager.SendSpawnItemForPlayer(player.Owner, SpawnUtils.Consumables.LONG_RANGE_FLASHLIGHT);
        }
    }
    
    public override void Enable()
    {
        _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        _gameManagerGO.SetActive(true);

        GameEvents.OnGameStateChanged += GameEvents_OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;

        LayerManager.LAYER_ENEMY = LayerManager.LAYER_PLAYER_SYNCED;

        AddAngySentries();
    }

    public override void Disable()
    {
        _gameManagerGO.SetActive(false);
        _harmonyInstance.UnpatchSelf();
        GameEvents.OnGameStateChanged -= GameEvents_OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams -= OnPlayerChangedTeams;

        LayerManager.MASK_MELEE_ATTACK_TARGETS = DEFAULT_MASK_MELEE_ATTACK_TARGETS;
        LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = DEFAULT_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

        LayerManager.MASK_BULLETWEAPON_RAY = DEFAULT_MASK_BULLETWEAPON_RAY;
        LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = DEFAULT_MASK_BULLETWEAPON_PIERCING_PASS;

        LayerManager.LAYER_ENEMY = LayerMask.NameToLayer("Enemy");

        RemoveAngySentries();
    }

    private static string HiderExtraInfoUpdater(PlayerWrapper player)
    {
        if (!player.IsLocal)
            return null;
        
        return GetZoneAndAreaInfo(player, "color=green");
    }
    
    private static string SeekersExtraInfoUpdater(PlayerWrapper player)
    {
        if (!player.CanBeSeenByLocalPlayer())
            return null;
        
        if (!player.IsLocal)
            return GetZoneAndAreaInfo(player);
        
        return GetZoneAndAreaInfo(player, "color=green");
    }

    private static string GetZoneAndAreaInfo(PlayerWrapper player, string color = "color=white")
    {
        var area = player.PlayerAgent.CourseNode?.m_area;

        if (area == null)
            return null;
        
        return $"<{color}>[<color=orange>ZONE {area.m_zone.NavInfo.Number}</color>, <color=orange>Area {area.m_navInfo.Suffix}</color>]";
    }
    
    private void GameEvents_OnGameStateChanged(eGameStateName state)
    {
        switch (state)
        {
            case eGameStateName.InLevel:
            {
                NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.PreGameAndOrSpectator);

                var teamDisplay = PUI_TeamDisplay.InstantiateOrGetInstanceOnWardenObjectives();
                teamDisplay.SetTeamDisplayData((int)GMTeam.Seekers, new('S', PUI_TeamDisplay.COLOR_RED, SeekersExtraInfoUpdater));
                teamDisplay.SetTeamDisplayData((int)GMTeam.Hiders, new('H', PUI_TeamDisplay.COLOR_CYAN, HiderExtraInfoUpdater));
                teamDisplay.UpdateTitle($"<color=orange><b>{DisplayName}</b></color>");

                WardenIntelOverride.ForceShowWardenIntel($"<size=200%><color=red>Special Warden Protocol\n<color=orange>{DisplayName}</color>\ninitialized.</color></size>");
        
                var localPlayer = PlayerManager.GetLocalPlayerAgent();

                if (localPlayer != null && localPlayer.Sound != null)
                {
                    localPlayer.Sound.Post(AK.EVENTS.ALARM_AMBIENT_STOP, Vector3.zero);
                    localPlayer.Sound.Post(AK.EVENTS.R8_REACTOR_ALARM_LOOP_STOP, Vector3.zero);
                }

                if (SNet.IsMaster)
                {
                    CoroutineManager.StartCoroutine(LightBringer().WrapToIl2Cpp());
                }

                PostLocalChatMessage(" ");
                PostLocalChatMessage("<align=center><color=orange><b><size=120%>Welcome to Hide and Seek!</align></color></b></size>");
                PostLocalChatMessage("---------------------------------------------------------------");
                PostLocalChatMessage("Use the <u>chat-commands</u> '<#f00>/seeker</color>' and '<#0ff>/hider</color>'");
                PostLocalChatMessage("to assign yourself to the two teams.");
                PostLocalChatMessage("---------------------------------------------------------------");
                PostLocalChatMessage("<#f00>Host only:</color>");
                PostLocalChatMessage("Use the command '<color=orange>/hnsstart</color>' to start the game.");
                PostLocalChatMessage("<#888>You can use '<color=orange>/hnsstop</color>' to end an active game at any time.</color>");
                PostLocalChatMessage("---------------------------------------------------------------");
                break;
            }
            case eGameStateName.Lobby:
            {
                PUI_TeamDisplay.DestroyInstanceOnWardenObjectives();

                if (SNet.IsMaster && NetSessionManager.HasSession)
                {
                    NetSessionManager.SendStopGamePacket();
                }

                break;
            }
        }
    }

    private void OnPlayerChangedTeams(PlayerWrapper playerInfo, int teamInt)
    {
        GMTeam team = (GMTeam)teamInt;

        if (!NetworkingManager.InLevel)
            return;

        if (!playerInfo.HasAgent)
            return;

        var storage = playerInfo.PlayerAgent.gameObject.GetOrAddComponent<PaletteStorage>();

        playerInfo.PlayerAgent.PlayerSyncModel.SetHelmetLightIntensity(0.1f);

        var syncModel = playerInfo.PlayerAgent.PlayerSyncModel;

        SetHelmetLights(syncModel);

        switch (team)
        {
            case GMTeam.Seekers:
                StoreOriginalAndAssignCustomPalette(playerInfo, storage, _seekerPalette);

                SetHelmetLights(syncModel, SEEKER_LIGHT_INTENSITY, SEEKER_LIGHT_RANGE, SEEKER_LIGHT_COLOR);

                if (playerInfo.IsLocal)
                {
                    LayerManager.MASK_MELEE_ATTACK_TARGETS = MODIFIED_MASK_MELEE_ATTACK_TARGETS;
                    LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = MODIFIED_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

                    LayerManager.MASK_BULLETWEAPON_RAY = DEFAULT_MASK_BULLETWEAPON_RAY;
                    LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = DEFAULT_MASK_BULLETWEAPON_PIERCING_PASS;

                    if (!GameManager.OnLocalPlayerCaught())
                    {
                        GameManager.ReviveLocalPlayer();
                    }

                    SetLocalPlayerStatusUIElementsActive(isSeeker: true);
                    SetNearDeathAudioLimit(playerInfo.PlayerAgent.Cast<LocalPlayerAgent>(), false);
                }

                if (SNet.IsMaster && NetSessionManager.HasSession && NetSessionManager.CurrentSession.SetupTimeFinished)
                {
                    NetworkingManager.PostChatLog($"{playerInfo.PlayerColorTag}{playerInfo.NickName} <#F00>has been caught!</color>");
                }

                break;
            default:
            case GMTeam.PreGameAndOrSpectator:
                StoreOriginalAndAssignCustomPalette(playerInfo, storage, _spectatorPalette);

                if (playerInfo.IsLocal)
                {
                    // Idk, it's bonking time xd
                    LayerManager.MASK_MELEE_ATTACK_TARGETS = MODIFIED_MASK_MELEE_ATTACK_TARGETS;
                    LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = MODIFIED_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

                    LayerManager.MASK_BULLETWEAPON_RAY = DEFAULT_MASK_BULLETWEAPON_RAY;
                    LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = DEFAULT_MASK_BULLETWEAPON_PIERCING_PASS;

                    InstantReviveLocalPlayer();

                    SetLocalPlayerStatusUIElementsActive(isSeeker: false);
                    SetNearDeathAudioLimit(playerInfo.PlayerAgent.Cast<LocalPlayerAgent>(), true);
                }
                break;
            case GMTeam.Hiders:
                RevertToOriginalPalette(playerInfo, storage);

                if (playerInfo.IsLocal)
                {
                    LayerManager.MASK_MELEE_ATTACK_TARGETS = DEFAULT_MASK_MELEE_ATTACK_TARGETS;
                    LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = DEFAULT_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

                    LayerManager.MASK_BULLETWEAPON_RAY = MODIFIED_MASK_BULLETWEAPON_RAY;
                    LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = MODIFIED_MASK_BULLETWEAPON_PIERCING_PASS;

                    SetLocalPlayerStatusUIElementsActive(isSeeker: false);
                    SetNearDeathAudioLimit(playerInfo.PlayerAgent.Cast<LocalPlayerAgent>(), true);
                }
                break;
        }

        EndGameCheck();

        // Defaults:
        // Range: 0.06
        // Intensity: 0.8
        // Color: 0.6471 0.7922 0.6824 1

        // Set Seekers Palette / Helmet light lol
        // Range: 0.2
        // Intensity: 5
        // Red flashlight in third person? hmmm
    }

    private static readonly Color HELMET_LIGHT_DEFAULT_COLOR = new Color(0.6471f, 0.7922f, 0.6824f, 1);

    private static void SetHelmetLights(PlayerSyncModelData syncModel, float intensity = 0.8f, float range = 0.06f, Color? color = null)
    {
        color ??= HELMET_LIGHT_DEFAULT_COLOR;

        foreach (var kvp in syncModel.m_helmetLights)
        {
            var light = kvp.Key;
            if (light == null)
                continue;

            if (light.name.Contains("Flashlight"))
                continue;

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

    private static void EndGameCheck()
    {
        if (!SNet.IsMaster)
            return;

        if (!NetSessionManager.HasSession)
            return;

        if (NetworkingManager.AllValidPlayers.Any(pl => pl.Team != (int)GMTeam.Seekers))
            return;

        NetSessionManager.SendStopGamePacket();
    }

    public static void SetNearDeathAudioLimit(LocalPlayerAgent player, bool enable)
    {
        // Not even sure if this works lol
        var localDamage = player.Damage.Cast<Dam_PlayerDamageLocal>();

        player.Breathing.enabled = enable;

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

    private const string PREFIX_ANGY_SENTRY = "Angry_";

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

            SetupAngySentry(block, angyName);
            refreshGear = true;

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
}