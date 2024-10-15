using Gamemodes;
using Gamemodes.Extensions;
using Gamemodes.Mode;
using Gamemodes.Net;
using Gamemodes.Patches.Required;
using HarmonyLib;
using HNS.Components;
using HNS.Net;
using HNS.Patches;
using Il2CppInterop.Runtime.Injection;
using Player;
using SNetwork;
using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HNS.Core;

internal class HideAndSeekMode : GamemodeBase
{
    public static HideAndSeekGameManager GameManager { get; private set; }

    public override string ID => "hideandseek";

    public override string DisplayName => "Hide and Seek";

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
    };

    private Harmony _harmonyInstance;

    private GameObject _gameManagerGO;

    public override void Init()
    {
        _harmonyInstance = new Harmony(Plugin.GUID);
        NetSessionManager.Init();

        ChatCommands.Add("hnsstart", StartHNS)
            .Add("hnsstop", StopHNS)
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

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<HideAndSeekGameManager>())
        {
            ClassInjector.RegisterTypeInIl2Cpp<HideAndSeekGameManager>();
            ClassInjector.RegisterTypeInIl2Cpp<PaletteStorage>();
            ClassInjector.RegisterTypeInIl2Cpp<PUI_TeamDisplay>();
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

        GameManager = _gameManagerGO.AddComponent<HideAndSeekGameManager>();
    }

    private static string SwitchToLobby(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.PreGameAndOrSpectator);
        return string.Empty;
    }

    private static string SwitchToHider(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.Hiders);
        return string.Empty;
    }

    private static string SwitchToSeeker(string[] arg)
    {
        if (NetSessionManager.HasSession)
            return "Can't switch teams mid session.";

        NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.Seekers);
        return string.Empty;
    }

    private static ClothesPalette seekerPalette;
    private static ClothesPalette spectatorPalette;

    private static void CreateSeekerPalette()
    {
        CreatePalette("SeekerPalette", Color.red, 6, out seekerPalette);
        CreatePalette("SpectatorPalette", Color.cyan, 9, out spectatorPalette);
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

    public static string StartHNS(string[] args)
    {
        var seekers = NetworkingManager.AllValidPlayers.Where(pw => pw.Team == (int)GMTeam.Seekers).Select(pw => pw.ID).ToArray();

        NetSessionManager.SendStartGamePacket(seekers);

        return $"Start Game Packet sent! -> {seekers.Length} Seekers";
    }

    public static string StopHNS(string[] args)
    {
        NetSessionManager.SendStopGamePacket();
        return "";
    }

    private static int DEFAULT_MASK_MELEE_ATTACK_TARGETS;
    private static int DEFAULT_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

    private static int DEFAULT_MASK_BULLETWEAPON_RAY;
    private static int DEFAULT_MASK_BULLETWEAPON_PIERCING_PASS;


    private static int MODIFIED_MASK_MELEE_ATTACK_TARGETS;
    private static int MODIFIED_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

    private static int MODIFIED_MASK_BULLETWEAPON_RAY;
    private static int MODIFIED_MASK_BULLETWEAPON_PIERCING_PASS;

    public override void Enable()
    {
        _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        _gameManagerGO.SetActive(true);

        GameEvents.OnGameStateChanged += GameEvents_OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;
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
    }

    private void GameEvents_OnGameStateChanged(eGameStateName state)
    {
        if (state == eGameStateName.InLevel)
        {
            NetworkingManager.AssignTeam(SNet.LocalPlayer, (int)GMTeam.PreGameAndOrSpectator);

            var go = GuiManager.PlayerLayer.WardenObjectives.gameObject;

            if (go.GetComponent<PUI_TeamDisplay>() == null)
            {
                var teamDisplay = go.AddComponent<PUI_TeamDisplay>();

                teamDisplay.UpdateTitle($"<color=orange><b>{DisplayName}</b></color>");
            }

            WardenIntelOverride.ForceShowWardenIntel($"<size=200%><color=red>Special Warden Protocol\n<color=orange>{DisplayName}</color>\ninitialized.</color></size>");
        
            var localPlayer = PlayerManager.GetLocalPlayerAgent();

            if (localPlayer != null && localPlayer.Sound != null)
            {
                localPlayer.Sound.Post(AK.EVENTS.ALARM_AMBIENT_STOP, Vector3.zero);
                localPlayer.Sound.Post(AK.EVENTS.R8_REACTOR_ALARM_LOOP_STOP, Vector3.zero);
            }
        }

        if (state == eGameStateName.Lobby)
        {
            var go = GuiManager.PlayerLayer.WardenObjectives.gameObject;
            var ui = go.GetComponent<PUI_TeamDisplay>();
            if (ui != null)
            {
                UnityEngine.Object.Destroy(ui);
            }

            if (SNet.IsMaster && NetSessionManager.HasSession)
            {
                NetSessionManager.SendStopGamePacket();
            }
        }
    }

    private static float ORIGINAL_m_nearDeathAudioLimit = -1;

    private void OnPlayerChangedTeams(PlayerWrapper playerInfo, int teamInt)
    {
        GMTeam team = (GMTeam)teamInt;

        if (!NetworkingManager.InLevel)
            return;

        if (!playerInfo.HasAgent)
            return;

        var storage = playerInfo.PlayerAgent.gameObject.GetOrAddComponent<PaletteStorage>();

        playerInfo.PlayerAgent.PlayerSyncModel.SetHelmetLightIntensity(0.1f);

        switch (team)
        {
            case GMTeam.Seekers:
                StoreOriginalAndAssignCustomPalette(playerInfo, storage, seekerPalette);

                playerInfo.PlayerAgent.PlayerSyncModel.SetHelmetLightIntensity(5);

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
                StoreOriginalAndAssignCustomPalette(playerInfo, storage, spectatorPalette);

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

        // Set Seekers Palette / Helmet light lol
        // Range: 0.2
        // Intensity: 5
        // Red flashlight in third person? hmmm
    }

    private static void StoreOriginalAndAssignCustomPalette(PlayerWrapper info, PaletteStorage storage, ClothesPalette paletteToSet)
    {
        if (storage.hiderPalette == null)
        {
            storage.hiderPalette = info.PlayerAgent.RigSwitch.m_currentPalette;
        }

        if (paletteToSet == null)
        {
            paletteToSet = seekerPalette;
        }

        info.PlayerAgent.RigSwitch.ApplyPalette(paletteToSet);
    }

    private static void RevertToOriginalPalette(PlayerWrapper info, PaletteStorage storage)
    {
        if (storage.hiderPalette != null)
        {
            info.PlayerAgent.RigSwitch.ApplyPalette(storage.hiderPalette);
            storage.hiderPalette = null;
        }
    }

    private void EndGameCheck()
    {
        if (!SNet.IsMaster)
            return;

        if (!NetSessionManager.HasSession)
            return;

        if (!NetworkingManager.AllValidPlayers.All(pl => pl.Team == (int)GMTeam.Seekers))
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

    public static void InstantReviveLocalPlayer()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return;

        var ploc = localPlayer.Locomotion;

        if (ploc.m_currentStateEnum == PlayerLocomotion.PLOC_State.Downed)
        {
            ploc.ChangeState(PlayerLocomotion.PLOC_State.Stand, wasWarpedIntoState: false);
        }

        localPlayer.Damage.AddHealth(localPlayer.Damage.HealthMax, localPlayer);
    }
}