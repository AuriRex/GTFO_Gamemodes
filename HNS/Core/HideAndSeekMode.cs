using Gamemodes;
using Gamemodes.Extensions;
using Gamemodes.Core;
using Gamemodes.Net;
using HarmonyLib;
using HNS.Components;
using HNS.Net;
using HNS.Patches;
using Il2CppInterop.Runtime.Injection;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Agents;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Components;
using Gamemodes.Core.Voice;
using Gamemodes.Core.Voice.Modulators;
using Gamemodes.UI.Menu;
using HNS.Extensions;
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
    
    private const float TEAM_MUTED = 0.725f;
    
    public static readonly Color COLOR_MUTED_TEAM_ALPHA = new(Color.red.r * TEAM_MUTED, Color.red.g * TEAM_MUTED, Color.red.b * TEAM_MUTED, PUI_TeamDisplay.COLOR_OPACITY);
    public static readonly Color COLOR_MUTED_TEAM_BETA = new(Color.blue.r * TEAM_MUTED, Color.blue.g * TEAM_MUTED, Color.blue.b * TEAM_MUTED, PUI_TeamDisplay.COLOR_OPACITY);
    public static readonly Color COLOR_MUTED_TEAM_GAMMA = new(Color.green.r * TEAM_MUTED, Color.green.g * TEAM_MUTED, Color.green.b * TEAM_MUTED, PUI_TeamDisplay.COLOR_OPACITY);
    public static readonly Color COLOR_MUTED_TEAM_DELTA = new(Color.magenta.r * TEAM_MUTED, Color.magenta.g * TEAM_MUTED, Color.magenta.b * TEAM_MUTED, PUI_TeamDisplay.COLOR_OPACITY);

    
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
    
    private static ClothesPalette _palette_teamAlpha;
    private static ClothesPalette _palette_teamBeta;
    private static ClothesPalette _palette_teamGamma;
    private static ClothesPalette _palette_teamDelta;
    
    private static float ORIGINAL_m_nearDeathAudioLimit = -1;

    public static bool IsTeamGame => NetworkingManager.AllValidPlayers.Any(pi => pi.Team >= (int)GMTeam.PreGameAlpha);

    private static readonly Color HELMET_LIGHT_DEFAULT_COLOR = new Color(0.6471f, 0.7922f, 0.6824f, 1);
    
    private static float SEEKER_LIGHT_INTENSITY = 5;
    private static float SEEKER_LIGHT_RANGE = 0.15f;
    private static Color SEEKER_LIGHT_COLOR = Color.red;

    private Sprite _icon;
    private Sprite _banner;
    private VolumeModulatorStack _vvmStack;
    
    private static readonly List<PackInfo> _originalResourcePackLocations = new();

    private static CustomGearSelector _gearMeleeSelector;
    private static CustomGearSelector _gearHiderSelector;
    private static CustomGearSelector _gearSeekerSelector;
    
    private static DateTimeOffset _pickToolCooldownEnd = DateTimeOffset.UtcNow;
    private static bool IsLocalPlayerAllowedToPickTool => DateTimeOffset.UtcNow > _pickToolCooldownEnd;

    private static readonly TimeKeeper _timeKeeper = new();

    internal static HNSTeam _localTeam;
    
    public override void Init()
    {
        _harmonyInstance = new Harmony(Plugin.GUID);
        NetSessionManager.Init(Net);

        ChatCommands
            .Add("hnshelp", SendHelpMessage)
            .Add("hnsstart", StartGame)
            .Add("hnsstop", StopGame)
            .Add("hnsabort", AbortGame)
            .Add("hnsteam", AssignHideAndSeekTeam)
            .Add("seeker", SwitchToSeeker)
            .Add("hider", SwitchToHider)
            .Add("lobby", SwitchToLobby)
            .Add("camera", SwitchToSpectator)
            .Add("spectate", ToggleSpectatorMode)
            .Add("melee", SelectMelee)
            .Add("tool", SelectTool)
            .Add("disinfect", Disinfect)
            .Add("dimension", JumpDimension)
            .Add("time", PrintTimes)
            .Add("total", PrintTimes)
            .Add("unstuck", Unstuck)
            .Add("fogtest", FogTest)
            .LogAnyErrors(Plugin.L.LogError, Plugin.L.LogWarning);

        CreateCustomPalettes();

        TeamVisibility.Team(GMTeam.Seekers).CanSeeSelf();

        var allSeekerTeams = new GMTeam[]
        {
            GMTeam.SeekerAlpha, GMTeam.SeekerBeta, GMTeam.SeekerGamma, GMTeam.SeekerDelta,
        };
        
        var allHiderTeams = new GMTeam[]
        {
            GMTeam.HiderAlpha, GMTeam.HiderBeta, GMTeam.HiderGamma, GMTeam.HiderDelta,
        };
        
        var allPreGameTeams = new GMTeam[]
        {
            GMTeam.PreGameAlpha, GMTeam.PreGameBeta, GMTeam.PreGameGamma, GMTeam.PreGameDelta,
        };

        var everything = new GMTeam[]
        {
            GMTeam.Seekers, GMTeam.Hiders, GMTeam.PreGame,
        };
        
        everything = everything.Concat(allSeekerTeams).Concat(allHiderTeams).Concat(allPreGameTeams).ToArray();
        
        // For Team games
        TeamVisibility.Team(GMTeam.SeekerAlpha).CanSee(allSeekerTeams).And(GMTeam.HiderAlpha);
        TeamVisibility.Team(GMTeam.HiderAlpha).CanSeeSelf().And(GMTeam.SeekerAlpha);
        
        TeamVisibility.Team(GMTeam.SeekerBeta).CanSee(allSeekerTeams).And(GMTeam.HiderBeta);
        TeamVisibility.Team(GMTeam.HiderBeta).CanSeeSelf().And(GMTeam.SeekerBeta);
        
        TeamVisibility.Team(GMTeam.SeekerGamma).CanSee(allSeekerTeams).And(GMTeam.HiderGamma);
        TeamVisibility.Team(GMTeam.HiderGamma).CanSeeSelf().And(GMTeam.SeekerGamma);
        
        TeamVisibility.Team(GMTeam.SeekerDelta).CanSee(allSeekerTeams).And(GMTeam.HiderDelta);
        TeamVisibility.Team(GMTeam.HiderDelta).CanSeeSelf().And(GMTeam.SeekerDelta);
        
        // Other Teams
        TeamVisibility.Team(GMTeam.PreGame).CanSee(everything);
        TeamVisibility.Team(GMTeam.PreGameAlpha).CanSee(everything);
        TeamVisibility.Team(GMTeam.PreGameBeta).CanSee(everything);
        TeamVisibility.Team(GMTeam.PreGameGamma).CanSee(everything);
        TeamVisibility.Team(GMTeam.PreGameDelta).CanSee(everything);
        
        TeamVisibility.Team(GMTeam.Camera)
            .WithLocalPlayerIconsHidden()
            .CanSee(everything);

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<PaletteStorage>())
        {
            ClassInjector.RegisterTypeInIl2Cpp<PaletteStorage>();
            ClassInjector.RegisterTypeInIl2Cpp<CustomMineController>();
            ClassInjector.RegisterTypeInIl2Cpp<PlayerTrackerController>();
            ClassInjector.RegisterTypeInIl2Cpp<XRayInstance>();
            
            ClassInjector.RegisterTypeInIl2Cpp<SpectatorController>();
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

        GameManager = new HideAndSeekGameManager(gameObject.AddComponent<TimerHUD>(), _timeKeeper);
        
        ImageLoader.LoadNewImageSprite(Resources.Data.HNS_Icon, out _icon);
        ImageLoader.LoadNewImageSprite(Resources.Data.HNS_Banner, out _banner);
        
        MineDeployerInstance_UpdateDetection_Patch.OnAgentDetected += OnMineDetectAgent;

        _vvmStack = new VolumeModulatorStack(new LobbySetMaxModulator(), new SpectatorVolumeMax(), new PlayerDeadModulator());
        
        PlayerTrackerController.OnStartedScanning += PlayerTrackerControllerOnOnStartedScanning;
    }

    public override Color? GetElevatorColor() => new Color(1f, 0.5f, 0f, 1f) * 0.5f;

    private static void CreateCustomPalettes()
    {
        CreatePalette("SeekerPalette", Color.red, 6, out _seekerPalette);
        CreatePalette("SpectatorPalette", Color.cyan, 9, out _spectatorPalette);
        
        CreatePalette($"Team{nameof(HNSTeam.Alpha)}Palette", Color.red, 7, out _palette_teamAlpha);
        CreatePalette($"Team{nameof(HNSTeam.Beta)}Palette", Color.blue, 8, out _palette_teamBeta);
        CreatePalette($"Team{nameof(HNSTeam.Gamma)}Palette", Color.green, 9, out _palette_teamGamma);
        CreatePalette($"Team{nameof(HNSTeam.Delta)}Palette", Color.magenta, 10, out _palette_teamDelta);
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
        
        _timeKeeper?.ClearSessions();
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

    private void GameEvents_OnGameStateChanged(eGameStateName state)
    {
        switch (state)
        {
            case eGameStateName.InLevel:
            {
                // Delay a single frame to prevent issues when late joining.
                CoroutineManager.StartCoroutine(Coroutines.NextFrame(() =>
                {
                    var team = NetSessionManager.HasSession ? GMTeam.Seekers : GMTeam.PreGame;
                    Plugin.L.LogDebug($"Assigning to team {team}");
                    NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)team);

                    var teamDisplay = PUI_TeamDisplay.InstantiateOrGetInstanceOnWardenObjectives();
                    teamDisplay.SetTeamDisplayData((int)GMTeam.Seekers, new("[S]  ", PUI_TeamDisplay.COLOR_RED, SeekersExtraInfoUpdater));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.Hiders, new("[<color=orange>H</color>]  ", PUI_TeamDisplay.COLOR_CYAN, HiderExtraInfoUpdater));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.Camera, new(null, PUI_TeamDisplay.COLOR_MISC, Hide: true));
                    
                    teamDisplay.SetTeamDisplayData((int)GMTeam.SeekerAlpha, new("[S] (A)  ", COLOR_MUTED_TEAM_ALPHA, SeekersExtraInfoUpdater));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.HiderAlpha, new("[<color=orange>H</color>] (A)  ", PUI_TeamDisplay.COLOR_RED, HiderExtraInfoUpdater));
                    
                    teamDisplay.SetTeamDisplayData((int)GMTeam.SeekerBeta, new("[S] (B)  ", COLOR_MUTED_TEAM_BETA, SeekersExtraInfoUpdater));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.HiderBeta, new("[<color=orange>H</color>] (B)  ", PUI_TeamDisplay.COLOR_BLUE, HiderExtraInfoUpdater));
                    
                    teamDisplay.SetTeamDisplayData((int)GMTeam.SeekerGamma, new("[S] (C)  ", COLOR_MUTED_TEAM_GAMMA, SeekersExtraInfoUpdater));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.HiderGamma, new("[<color=orange>H</color>] (C)  ", PUI_TeamDisplay.COLOR_GREEN, HiderExtraInfoUpdater));
                    
                    teamDisplay.SetTeamDisplayData((int)GMTeam.SeekerDelta, new("[S] (D)  ", COLOR_MUTED_TEAM_DELTA, SeekersExtraInfoUpdater));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.HiderDelta, new("[<color=orange>H</color>] (D)  ", PUI_TeamDisplay.COLOR_MAGENTA, HiderExtraInfoUpdater));
                    
                    teamDisplay.SetTeamDisplayData((int)GMTeam.PreGameAlpha, new("[Team A]  ", PUI_TeamDisplay.COLOR_RED));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.PreGameBeta, new("[Team B]  ", PUI_TeamDisplay.COLOR_BLUE));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.PreGameGamma, new("[Team C]  ", PUI_TeamDisplay.COLOR_GREEN));
                    teamDisplay.SetTeamDisplayData((int)GMTeam.PreGameDelta, new("[Team D]  ", PUI_TeamDisplay.COLOR_MAGENTA));
                    
                    teamDisplay.UpdateTitle($"<color=orange><b>{DisplayName}</b></color>");

                    WardenIntelOverride.ForceShowWardenIntel($"<size=200%><color=red>Special Warden Protocol\n<color=orange>{DisplayName}</color>\ninitialized.</color></size>");
        
                    var localPlayer = PlayerManager.GetLocalPlayerAgent();

                    if (localPlayer != null && localPlayer.Sound != null)
                    {
                        localPlayer.Sound.Post(AK.EVENTS.ALARM_AMBIENT_STOP, Vector3.zero);
                        localPlayer.Sound.Post(AK.EVENTS.R8_REACTOR_ALARM_LOOP_STOP, Vector3.zero);
                    }

                    HideResourcePacks();
                
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

    private void OnPlayerChangedTeams(PlayerWrapper playerInfo, int teamInt)
    {
        GMTeam team = (GMTeam)teamInt;

        var teamDisplay = PUI_TeamDisplay.InstantiateOrGetInstanceOnWardenObjectives();
        var extraText = string.Empty;
        if (IsTeamGame)
        {
            extraText = " <#555>://</color> <color=orange><i>Teams</i></color>";
        }
        teamDisplay.UpdateTitle($"<color=orange><b>{DisplayName}</b></color>{extraText}");
        
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
        else if (!playerInfo.PlayerAgent.PlayerSyncModel.gameObject.activeSelf)
        {
            playerInfo.PlayerAgent.PlayerSyncModel.gameObject.SetActive(true);
        }

        if (TeamHelper.IsSeeker(team))
        {
            team = GMTeam.Seekers;
        }
        else if (TeamHelper.IsHider(team))
        {
            team = GMTeam.Hiders;
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
            case GMTeam.PreGame:
                var palette = GetLobbyPalette((GMTeam) playerInfo.Team);
                StoreOriginalAndAssignCustomPalette(playerInfo, storage, palette);

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
            case GMTeam.Camera:
                if (playerInfo.IsLocal)
                {
                    LayerManager.MASK_MELEE_ATTACK_TARGETS = DEFAULT_MASK_MELEE_ATTACK_TARGETS;
                    LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = DEFAULT_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

                    LayerManager.MASK_BULLETWEAPON_RAY = MODIFIED_MASK_BULLETWEAPON_RAY;
                    LayerManager.MASK_BULLETWEAPON_PIERCING_PASS = MODIFIED_MASK_BULLETWEAPON_PIERCING_PASS;

                    InstantReviveLocalPlayer();

                    SetLocalPlayerStatusUIElementsActive(isSeeker: false);
                    SetNearDeathAudioLimit(playerInfo.PlayerAgent.Cast<LocalPlayerAgent>(), true);
                    break;
                }
                
                playerInfo.PlayerAgent.PlayerSyncModel.gameObject.SetActive(false);
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

    private static bool EndGameCheck()
    {
        if (!SNet.IsMaster)
            return false;

        if (!NetSessionManager.HasSession)
            return false;

        if (NetworkingManager.AllValidPlayers
            .Where(pl => pl.Team != (int) GMTeam.Camera)
            .Any(pl => TeamHelper.IsHider((GMTeam) pl.Team)))
            return false;

        NetSessionManager.SendStopGamePacket();
        return true;
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

    private void OnMineDetectAgent(MineDeployerInstance mine, Agent agent)
    {
        var localPlayer = agent?.TryCast<LocalPlayerAgent>();

        if (localPlayer == null)
            return;

        var team = TeamHelper.SimplifyTeam((GMTeam)NetworkingManager.LocalPlayerTeam);

        switch (team)
        {
            case GMTeam.Hiders:
            case GMTeam.Seekers:
                break;
            default:
                return;
        }
        
        mine.GetController().DetectedLocalPlayer();
    }
}