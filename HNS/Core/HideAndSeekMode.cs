using Gamemodes;
using Gamemodes.Extensions;
using Gamemodes.Mode;
using Gamemodes.Net;
using Gamemodes.Patches.Required;
using HarmonyLib;
using HNS.Components;
using HNS.Net;
using Il2CppInterop.Runtime.Injection;
using SNetwork;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HNS.Core;

internal class HideAndSeekMode : GamemodeBase
{
    public static HideAndSeekGameManager GameManager { get; private set; }

    public override string ID => "hideandseek";

    public override string DisplayName => "Hide 'n' Seek";

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

        ChatCommandsHandler.AddCommand("hnsstart", StartHNS);
        ChatCommandsHandler.AddCommand("hnsstop", StopHNS);

        CreateSeekerPalette();

        TeamVisibility.Team((int)GMTeam.Seekers).CanSeeSelf();

#warning TODO: Remove this here later!
        TeamVisibility.Team((int)GMTeam.Hiders).CanSeeSelf();

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

        GameManager = _gameManagerGO.AddComponent<HideAndSeekGameManager>();
    }

    private static ClothesPalette seekerPalette;

    private static void CreateSeekerPalette()
    {
        var go = new GameObject("SeekerPalette");

        go.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;
        GameObject.DontDestroyOnLoad(go);

        seekerPalette = go.AddComponent<ClothesPalette>();

        var tone = new ClothesPalette.Tone()
        {
            m_color = Color.red,
            m_materialOverride = 6,
            m_texture = Texture2D.whiteTexture,
        };

        seekerPalette.m_textureTiling = 1;
        seekerPalette.m_primaryTone = tone;
        seekerPalette.m_secondaryTone = tone;
        seekerPalette.m_tertiaryTone = tone;
        seekerPalette.m_quaternaryTone = tone;
        seekerPalette.m_quinaryTone = tone;
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

    private static int MODIFIED_MASK_MELEE_ATTACK_TARGETS;
    private static int MODIFIED_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

    public override void Enable()
    {
        _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        _gameManagerGO.SetActive(true);

        GameEvents.OnGameSessionStart += GameEvents_OnGameSessionStart;
        GameEvents.OnGameStateChanged += GameEvents_OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;
    }

    

    public override void Disable()
    {
        _gameManagerGO.SetActive(false);
        _harmonyInstance.UnpatchSelf();

        GameEvents.OnGameSessionStart -= GameEvents_OnGameSessionStart;
        GameEvents.OnGameStateChanged -= GameEvents_OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams -= OnPlayerChangedTeams;

        LayerManager.MASK_MELEE_ATTACK_TARGETS = DEFAULT_MASK_MELEE_ATTACK_TARGETS;
        LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = DEFAULT_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;
    }

    private void GameEvents_OnGameStateChanged(eGameStateName state)
    {
        if (state == eGameStateName.InLevel)
        {
            
        }
    }

    private void OnPlayerChangedTeams(PlayerWrapper info, int teamInt)
    {
        GMTeam team = (GMTeam)teamInt;

        if (!NetworkingManager.InLevel)
            return;

        if (!info.HasAgent)
            return;

        var storage = info.PlayerAgent.gameObject.GetOrAddComponent<PaletteStorage>();

        switch (team)
        {
            case GMTeam.Seekers:
                if (storage.hiderPalette == null)
                {
                    storage.hiderPalette = info.PlayerAgent.RigSwitch.m_currentPalette;
                }

                info.PlayerAgent.RigSwitch.ApplyPalette(seekerPalette);

                if (info.IsLocal)
                {
                    LayerManager.MASK_MELEE_ATTACK_TARGETS = MODIFIED_MASK_MELEE_ATTACK_TARGETS;
                    LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = MODIFIED_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

                    GameManager.OnLocalPlayerCaught();
                }

                if (SNet.IsMaster && NetSessionManager.HasSession && NetSessionManager.CurrentSession.SetupTimeFinished)
                {
                    NetworkingManager.PostChatLog($"{info.PlayerColorTag}{info.NickName} <#F00>has been caught!</color>");
                }

                break;
            case GMTeam.Hiders:
                if (storage.hiderPalette != null)
                {
                    info.PlayerAgent.RigSwitch.ApplyPalette(storage.hiderPalette);
                    storage.hiderPalette = null;
                }

                if (info.IsLocal)
                {
                    LayerManager.MASK_MELEE_ATTACK_TARGETS = DEFAULT_MASK_MELEE_ATTACK_TARGETS;
                    LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = DEFAULT_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;
                }
                break;
        }

        EndGameCheck();

        // Set Seekers Palette / Helmet light lol
        // Range: 0.2
        // Intensity: 5
        // Red flashlight in third person? hmmm
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

    private void GameEvents_OnGameSessionStart()
    {
        // TODO: Not this xd
        Plugin.L.LogWarning("Hi o/");
    }
}