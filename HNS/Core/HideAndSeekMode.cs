using Gamemodes;
using Gamemodes.Extensions;
using Gamemodes.Mode;
using Gamemodes.Net;
using Gamemodes.Patches.Required;
using HarmonyLib;
using HNS.Components;
using HNS.Net;
using Il2CppInterop.Runtime.Injection;
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
        /*        ChatCommandsHandler.AddCommand("hnsstart", ((Func<string[], string>)StartHNS).Method);
                ChatCommandsHandler.AddCommand("hnsstop", ((Func<string[], string>)StopHNS).Method);*/

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
        }

        GameManager = _gameManagerGO.AddComponent<HideAndSeekGameManager>();
    }

    private static ClothesPalette seekerPalette;

    private void CreateSeekerPalette()
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

    private static int PREV_MASK_MELEE_ATTACK_TARGETS;
    private static int PREV_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;

    public override void Enable()
    {
        _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        _gameManagerGO.SetActive(true);

        GameEvents.OnGameSessionStart += GameEvents_OnGameSessionStart;
        GameEvents.OnGameStateChanged += GameEvents_OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;

        PREV_MASK_MELEE_ATTACK_TARGETS = LayerManager.MASK_MELEE_ATTACK_TARGETS;
        LayerManager.MASK_MELEE_ATTACK_TARGETS = LayerManager.Current.GetMask(new string[]
        {
            "EnemyDamagable",
            "Dynamic",
            "PlayerSynced" // <-- Added
        });

        PREV_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;
        LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = LayerManager.Current.GetMask(new string[]
        {
            "EnemyDamagable",
            "Dynamic",
            "Default",
            "Default_NoGraph",
            "Default_BlockGraph",
            "EnemyDead",
            "PlayerSynced" // <-- Added
        });
    }

    

    public override void Disable()
    {
        _gameManagerGO.SetActive(false);
        _harmonyInstance.UnpatchSelf();

        GameEvents.OnGameSessionStart -= GameEvents_OnGameSessionStart;
        GameEvents.OnGameStateChanged -= GameEvents_OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams -= OnPlayerChangedTeams;

        LayerManager.MASK_MELEE_ATTACK_TARGETS = PREV_MASK_MELEE_ATTACK_TARGETS;
        LayerManager.MASK_MELEE_ATTACK_TARGETS_WITH_STATIC = PREV_MASK_MELEE_ATTACK_TARGETS_WITH_STATIC;
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
                break;
            case GMTeam.Hiders:
                if (storage.hiderPalette != null)
                {
                    info.PlayerAgent.RigSwitch.ApplyPalette(storage.hiderPalette);
                    storage.hiderPalette = null;
                }
                break;
        }

        if (info.IsLocal)
        {

        }

        // Set Seekers Palette / Helmet light lol
        // Range: 0.2
        // Intensity: 5
        // Red flashlight in third person? hmmm
    }

    private void GameEvents_OnGameSessionStart()
    {
        // TODO: Not this xd
        Plugin.L.LogWarning("Hi o/");
    }
}