using GameData;
using Gamemodes;
using Gamemodes.Extensions;
using Gamemodes.Core;
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
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using Agents;
using AIGraph;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Components;
using Gamemodes.Core.Voice;
using Gamemodes.Core.Voice.Modulators;
using Gamemodes.UI.Menu;
using HNS.Extensions;
using LevelGeneration;
using UnityEngine;
using PlayerVoiceManager = Gamemodes.Core.Voice.PlayerVoiceManager;

namespace HNS.Core;

internal partial class HideAndSeekMode : GamemodeBase
{
    public const string MODE_ID = "hideandseek";
    
    public static HideAndSeekGameManager GameManager { get; private set; }

    public override string ID => MODE_ID;

    public override string DisplayName => "Hide and Seek";

    public override string Description => "No Enemies\nAll Doors Open\n\n<#f00>Seekers</color> have to catch all <#0ff>Hiders</color>";

    public override Sprite SpriteLarge => _banner;
    
    public override Sprite SpriteSmall => _icon;
    
    private const string PREFIX_ANGY_SENTRY = "Angry_";
    private const float TOOL_SELECT_COOLDOWN = 60;
    private const float PUSH_FORCE_MULTI_DEFAULT = 2.5f;
    private const float PUSH_FORCE_MULTI_HIDER = -0.2f;
    
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
        InitialPushForceMultiplier = PUSH_FORCE_MULTI_DEFAULT,
        InitialSlidePushForceMultiplier = PUSH_FORCE_MULTI_DEFAULT,
        UseProximityVoiceChat = true,
    };

    private Harmony _harmonyInstance;

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

    private Sprite _icon;
    private Sprite _banner;
    private VolumeModulatorStack _vvmStack;
    private static List<PackInfo> _originalResourcePackLocations = new();

    private static CustomGearSelector _gearMeleeSelector;
    private static CustomGearSelector _gearHiderSelector;
    private static CustomGearSelector _gearSeekerSelector;
    
    private static DateTimeOffset _pickToolCooldownEnd = DateTimeOffset.UtcNow;
    private static bool IsLocalPlayerAllowedToPickTool => DateTimeOffset.UtcNow > _pickToolCooldownEnd;
    
    public override void Init()
    {
        _harmonyInstance = new Harmony(Plugin.GUID);
        NetSessionManager.Init(Net);

        ChatCommands
            .Add("hnshelp", SendHelpMessage)
            .Add("hnsstart", StartGame)
            .Add("hnsstop", StopGame)
            .Add("seeker", SwitchToSeeker)
            .Add("hider", SwitchToHider)
            .Add("lobby", SwitchToLobby)
            .Add("melee", SelectMelee)
            .Add("tool", SelectTool)
            .Add("disinfect", Disinfect)
            .Add("dimension", JumpDimension)
            .Add("unstuck", Unstuck)
            .LogAnyErrors(Plugin.L.LogError, Plugin.L.LogWarning);

        CreateSeekerPalette();

        TeamVisibility.Team(GMTeam.Seekers).CanSeeSelf();

        TeamVisibility.Team(GMTeam.PreGameAndOrSpectator).CanSeeSelf().And(GMTeam.Seekers, GMTeam.Hiders);

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<PaletteStorage>())
        {
            ClassInjector.RegisterTypeInIl2Cpp<PaletteStorage>();
            ClassInjector.RegisterTypeInIl2Cpp<CustomMineController>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerTrackerController>();
            ClassInjector.RegisterTypeInIl2Cpp<XRayInstance>();
        }
        
        XRayManager.Init();

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

        GameManager = new HideAndSeekGameManager(gameObject.AddComponent<TimerHUD>());
        
        ImageLoader.LoadNewImageSprite(Resources.Data.HNS_Icon, out _icon);
        ImageLoader.LoadNewImageSprite(Resources.Data.HNS_Banner, out _banner);
        
        MineDeployerInstance_UpdateDetection_Patch.OnAgentDetected += OnMineDetectAgent;

        _vvmStack = new VolumeModulatorStack(new LobbySetMaxModulator(), new SpectatorVolumeMax(), new PlayerDeadModulator());
        
        PlayerTrackerController.OnStartedScanning +=PlayerTrackerControllerOnOnStartedScanning;
    }

    private void PlayerTrackerControllerOnOnStartedScanning(PlayerAgent player, float cooldown)
    {
        if (!player.IsLocallyOwned)
            return;

        if (!NetSessionManager.HasSession)
            return;

        var newCooldownEnd = DateTimeOffset.UtcNow.AddSeconds(cooldown);

        if (_pickToolCooldownEnd < newCooldownEnd)
        {
            _pickToolCooldownEnd = newCooldownEnd;
        }
    }

    public override Color? GetElevatorColor() => new Color(1f, 0.5f, 0f, 1f) * 0.5f;

    private void OnMineDetectAgent(MineDeployerInstance mine, Agent agent)
    {
        var localPlayer = agent?.TryCast<LocalPlayerAgent>();

        if (localPlayer == null)
            return;

        if (NetworkingManager.LocalPlayerTeam == (int)GMTeam.PreGameAndOrSpectator)
            return;
        
        mine.GetController().DetectedLocalPlayer();
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
            
            NetworkingManager.SendSpawnItemForPlayer(player.Owner, PrefabManager.SpecialLRF_BlockID);
        }
    }
    
    public override void Enable()
    {
        _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        gameObject.SetActive(true);

        GameEvents.OnGameStateChanged += GameEvents_OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;

        LayerManager.LAYER_ENEMY = LayerManager.LAYER_PLAYER_SYNCED;

        AddAngySentries();

        SetupGearSelectors();

        PlayerVoiceManager.SetModulatorStack(_vvmStack);
    }

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
        switch ((GMTeam)NetworkingManager.GetLocalPlayerInfo().Team)
        {
            default:
            case GMTeam.PreGameAndOrSpectator:
                break;
            case GMTeam.Hiders:
                value = 0.125f;
                break;
            case GMTeam.Seekers:
                value = 0.125f * 3;
                break;
        }
            
        GearUtils.LocalReserveAmmoAction(GearUtils.AmmoType.Tool, GearUtils.AmmoAction.SetToPercent, value);
    }

    private static bool DoRefillGunsAndToolOnPick()
    {
        return !NetSessionManager.HasSession;
    }

    public override void Disable()
    {
        gameObject.SetActive(false);
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
        
        return $"<{color}>[<color=orange>ZONE {area.m_zone.NavInfo.Number}</color>, <color=orange>Area {area.m_navInfo.Suffix}</color>]";
    }
    
    private void GameEvents_OnGameStateChanged(eGameStateName state)
    {
        switch (state)
        {
            case eGameStateName.InLevel:
            {
                // Delay a single frame to prevent issues when late joining.
                CoroutineManager.StartCoroutine(Coroutines.NextFrame(() =>
                {
                    var team = NetSessionManager.HasSession ? GMTeam.Seekers : GMTeam.PreGameAndOrSpectator;
                    Plugin.L.LogDebug($"Assigning to team {team}");
                    NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)team);

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

                    HideResourcePacksAndSpawnFlashbangs();
                
                    if (SNet.IsMaster)
                    {
                        //TODO: Fix late join thingies
                        //CoroutineManager.StartCoroutine(LightBringer().WrapToIl2Cpp());
                    }

                    SendHelpMessage(Array.Empty<string>());
                    
                    // Fix late joiners being unable to move until they switch to their melee weapon once
                    PlayerManager.GetLocalPlayerAgent().EnemyCollision.m_moveSpeedModifier = 1f;
                }).WrapToIl2Cpp());
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
    
    public override void OnRemotePlayerEnteredLevel(PlayerWrapper player)
    {
        if (!SNet.IsMaster)
            return;
        
        CoroutineManager.StartCoroutine(Coroutines.DoAfter(1f, () =>
        {
            Plugin.L.LogWarning($"Spawning PLRF for player: {player.NickName}");
            NetworkingManager.SendSpawnItemForPlayer(player, PrefabManager.SpecialLRF_BlockID);
        }).WrapToIl2Cpp());
    }

    internal static void DespawnOldStuffs()
    {
        var mineInstances = ToolInstanceCaches.MineCache.All; //UnityEngine.Object.FindObjectsOfType<MineDeployerInstance>().ToArray();
        var cfoam = ToolInstanceCaches.GlueCache.All;// UnityEngine.Object.FindObjectsOfType<GlueGunProjectile>().ToArray();

        CoroutineManager.StartCoroutine(DespawnOldThings(cfoam, mineInstances).WrapToIl2Cpp());
    }
    
    private static IEnumerator DespawnOldThings(IEnumerable<GlueGunProjectile> cfoamBlobs, IEnumerable<MineDeployerInstance> mines)
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

    private static void HideResourcePacksAndSpawnFlashbangs()
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

    private static Coroutine _flashSpawnerRoutine;

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
    
    private class PackInfo
    {
        public ResourcePackPickup pickup;

        public Vector3 position;
        public AIG_CourseNode node;
        public LG_ResourceContainer_Storage container;

        private bool _lateSetup;
        
        public void LateSetup()
        {
            if (_lateSetup)
                return;

            _lateSetup = true;
            
            container ??= pickup.gameObject.GetComponentInParent<LG_ResourceContainer_Storage>();
            node ??= container.m_core.SpawnNode;
            
            foreach (var slot in container.m_storageSlots)
            {
                if (Vector3.Distance(slot.ResourcePack.position, position) > 0.05f)
                    continue;
                
                position = slot.Consumable.position;
                break;
            }
        }
    }
    
    private static IEnumerator SpawnFlashbangs()
    {
        if (!SNet.IsMaster)
            yield break;
        
        var stopWatch = Stopwatch.StartNew();
        Plugin.L.LogDebug("Spawning Flashbangs ...");
        var pickups = UnityEngine.Object.FindObjectsOfType<ConsumablePickup_Core>();
        
        var flashbangs = pickups.Where(p => p.name.Contains("Flashbang", StringComparison.InvariantCultureIgnoreCase)).ToArray();

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

        if (playerInfo.IsLocal)
        {
            SetPushForceMultiplierForLocalPlayer(PUSH_FORCE_MULTI_DEFAULT, PUSH_FORCE_MULTI_DEFAULT);
        }
        
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
                    
                    SetPushForceMultiplierForLocalPlayer(PUSH_FORCE_MULTI_HIDER, PUSH_FORCE_MULTI_DEFAULT);
                }
                break;
        }

        if (EndGameCheck())
            return;

        foreach (var mine in ToolInstanceCaches.MineCache.All)
        {
            mine.GetController()?.RefreshVisuals();
        }
        
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

    private static bool EndGameCheck()
    {
        if (!SNet.IsMaster)
            return false;

        if (!NetSessionManager.HasSession)
            return false;

        if (NetworkingManager.AllValidPlayers.Any(pl => pl.Team != (int)GMTeam.Seekers))
            return false;

        NetSessionManager.SendStopGamePacket();
        return true;
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