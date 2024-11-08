using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Net;
using Gamemodes.Patches.Required;
using Player;
using System;
using System.Collections.Generic;
using System.Linq;
using Gamemodes.Core.TestModes;
using static Gamemodes.PatchManager;
using Object = UnityEngine.Object;

namespace Gamemodes.Core;

public class GamemodeManager
{
    private static readonly HashSet<ModeInfo> _gamemodes = new();
    private static readonly HashSet<string> _gamemodeIds = new();

    public static IEnumerable<ModeInfo> LoadedModes => _gamemodes;
    public static IEnumerable<string> LoadedModeIds => _gamemodeIds;

    public static bool AllowDropWithVanillaPlayers => _currentMode?.GetType() == typeof(ModeGTFO);

    public static bool CurrentAllowsForcedTP => _currentMode?.Settings?.AllowForcedTeleportation ?? false;

    internal static GamemodeBase CurrentMode => _currentMode;
    internal static ModeSettings CurrentSettings => _currentMode?.Settings;

    private static GamemodeBase _currentMode;

    private static ModeInfo _modeGTFO;

    private static bool _isGamedataReady = false;

    public static int ModeSwitchCount { get; private set; }
    
    internal static event Action<ModeInfo> OnGamemodeChanged;
    public static bool IsVanilla => CurrentMode == null || CurrentMode.ID == ModeGTFO.MODE_ID;

    internal static void Init()
    {
        NetworkingManager.DoSwitchModeReceived += OnSwitchModeReceived;
        GameEvents.OnGameDataInit += OnGameDataInit;
        GameEvents.OnGameStateChanged += OnGameStateChanged;
        NetworkingManager.OnPlayerChangedTeams += OnPlayerChangedTeams;

        _modeGTFO = RegisterMode<ModeGTFO>();
#if DEBUG
        RegisterMode<ModeTesting>();
        RegisterMode<ModeNoSleepers>();
        RegisterMode<GtfoButTheLevelSplitsUp>();
#endif
    }

    private static void OnPlayerChangedTeams(PlayerWrapper info, int team)
    {
        PlayerManager.GetLocalPlayerAgent()?.TryCast<LocalPlayerAgent>()?.SetTeammateInfoVisible(true);
    }

    private static void OnGameStateChanged(eGameStateName state)
    {
        if (state == eGameStateName.InLevel)
        {
            HandleSpecialRequirementsOnInLevel();
        }
    }

    private static void OnGameDataInit()
    {
        foreach (var mode in _gamemodes)
        {
            try
            {
                mode.Implementation.Init();
            }
            catch (Exception ex)
            {
                Plugin.L.LogError($"Mode (ID:{mode.ID}) \"{mode.DisplayName}\" threw an Exception during {nameof(GamemodeBase.Init)}!");
                Plugin.L.LogError($"{ex.GetType().FullName}: {ex.Message}");
                Plugin.L.LogWarning($"StackTrace:\n{ex.StackTrace}");
            }
        }

        _isGamedataReady = true;

        TrySwitchMode(_modeGTFO);
    }

    private static void OnSwitchModeReceived(string gamemodeId)
    {
        TrySwitchMode(gamemodeId);
    }

    private static bool TrySwitchMode(ModeInfo mode)
    {
        if (mode?.ID == CurrentMode?.ID)
        {
            Plugin.L.LogDebug("Trying to switch to already loaded mode, ignoring.");
            return false;
        }

        if (NetworkingManager.InLevel)
        {
            if (!_currentMode.Settings.AllowMidGameModeSwitch)
            {
                Plugin.L.LogWarning($"Tried to switch from mode \"{_currentMode.ID}\" during gameplay, not allowed!");
                return false;
            }

            if (!mode.Implementation.Settings.AllowMidGameModeSwitch)
            {
                Plugin.L.LogWarning($"Tried to switch to mode \"{mode.ID}\" during gameplay, not allowed!");
                return false;
            }
        }
        
        Plugin.L.LogDebug($"Switching mode [{_currentMode?.ID ?? "None"}] => [{mode.ID}]");
        Plugin.PostLocalMessage($"<color=green>Switching to gamemode <color=orange>{mode.DisplayName}</color>!");

        try
        {
            _currentMode?.Disable();
        }
        catch (Exception ex)
        {
            Plugin.LogException(ex, $"Gamemode.{nameof(GamemodeBase.Disable)}()", printInChat: true);
        }
        
        CleanupSpecialRequirements(_currentMode?.Settings);
        
        _currentMode = mode.Implementation;
        ModeSwitchCount++;
        
        HandleSpecialRequirements(_currentMode.Settings);

        try
        {
            _currentMode.Enable();
        }
        catch (Exception ex)
        {
            Plugin.LogException(ex, $"Gamemode.{nameof(GamemodeBase.Disable)}()", printInChat: true);
        }

        try
        {
            OnGamemodeChanged?.Invoke(mode);
        }
        catch(Exception ex)
        {
            Plugin.LogException(ex, $"{nameof(OnGamemodeChanged)} event");
        }

        return true;
    }

    private static bool TrySwitchMode(string gamemodeID)
    {
        if (!TryGetMode(gamemodeID, out var mode))
        {
            Plugin.L.LogWarning($"Tried to switch to mode \"{gamemodeID}\" that is not installed!");
            return false;
        }

        return TrySwitchMode(mode);
    }

    private static void HandleSpecialRequirements(ModeSettings settings)
    {
        // Adds like 4 seconds of lag time due to the amount of patches ...
        //ApplyPatchGroup(PatchGroups.ACHIEVEMENT_PATCHES, !IsVanilla);
        
        ApplyPatchGroup(PatchGroups.NO_FAIL, settings.PreventDefaultFailState);
        ApplyPatchGroup(PatchGroups.NO_RESPAWN, settings.PreventRespawnRoomsRespawning);
        ApplyPatchGroup(PatchGroups.NO_SLEEPING_ENEMIES, settings.PreventExpeditionEnemiesSpawning);
        ApplyPatchGroup(PatchGroups.NO_CHECKPOINTS, settings.OpenAllSecurityDoors || settings.RemoveCheckpoints);
        ApplyPatchGroup(PatchGroups.NO_WORLDEVENTS, settings.BlockWorldEvents);
        ApplyPatchGroup(PatchGroups.FORCE_ARENA_DIM, settings.ForceAddArenaDimension);
        ApplyPatchGroup(PatchGroups.NO_VOICE, settings.DisableVoiceLines);
        ApplyPatchGroup(PatchGroups.USE_TEAM_VISIBILITY, settings.UseTeamVisibility);
        ApplyPatchGroup(PatchGroups.NO_TERMINAL_COMMANDS, settings.RemoveTerminalCommands);
        ApplyPatchGroup(PatchGroups.NO_BLOOD_DOORS, settings.RemoveBloodDoors);
        ApplyPatchGroup(PatchGroups.NO_PLAYER_REVIVE, settings.PreventPlayerRevives);
        ApplyPatchGroup(PatchGroups.INF_SENTRY_AMMO, settings.InfiniteSentryAmmo);
        ApplyPatchGroup(PatchGroups.INF_PLAYER_AMMO, settings.InfiniteBackpackAmmo);

        PushForcePatch.PushForceMultiplier = settings.InitialPushForceMultiplier;
        PushForcePatch.SlidePushForceMultiplier = settings.InitialSlidePushForceMultiplier;
    }

    private static void CleanupSpecialRequirements(ModeSettings settings)
    {
        if (settings == null)
            return;
        
        if (settings.PreventPlayerRevives)
        {
            foreach (var agent in Object.FindObjectsOfType<PlayerAgent>())
            {
                if (agent == null || agent.ReviveInteraction == null)
                    continue;
                
                agent.ReviveInteraction.gameObject.SetActive(true);
            }
        }
    }

    private static void HandleSpecialRequirementsOnInLevel()
    {
        if (CurrentSettings.BlockWorldEvents)
        {
            Utils.DisableAllWorldEventTriggers();
            Utils.StopWardenObjectiveManager();
        }

        if (CurrentSettings.RevealEntireMap)
        {
            Utils.RevealMap();

            if (CurrentSettings.MapIconsToReveal != Utils.MapIconTypes.None)
            {
                Utils.RevealMapIcons(CurrentSettings.MapIconsToReveal);
            }
        }

        if (CurrentSettings.PreventPlayerRevives)
        {
            foreach (var agent in PlayerManager.PlayerAgentsInLevel)
            {
                agent.ReviveInteraction.gameObject.SetActive(false);
            }
        }
        
        if (!SNetwork.SNet.IsMaster)
            return;

        // Master is in control of doors

        if (CurrentSettings.OpenAllSecurityDoors)
        {
            CoroutineManager.StartCoroutine(Utils.OpenSecurityDoorRoutine().WrapToIl2Cpp());
        }

        if (CurrentSettings.OpenAllWeakDoors)
        {
            CoroutineManager.StartCoroutine(Utils.OpenWeakDoorRoutine().WrapToIl2Cpp());
        }
    }

    public static ModeInfo RegisterMode<T>() where T : GamemodeBase
    {
        var impl = Activator.CreateInstance<T>();

        var id = impl.ID;
        var displayName = impl.DisplayName;

        if (_gamemodeIds.Contains(id))
            throw new ArgumentException($"Gamemode \"{id}\" can't be registered twice!", nameof(id));

        var info = new ModeInfo(id, displayName, impl);

        if (_isGamedataReady)
            impl.Init();

        _gamemodeIds.Add(id);
        _gamemodes.Add(info);

        return info;
    }

    public static bool HasModeInstalled(string gamemodeID)
    {
        return _gamemodeIds.Contains(gamemodeID);
    }

    public static bool TryGetMode(string gamemodeID, out ModeInfo mode)
    {
        mode = _gamemodes.FirstOrDefault(gm => gm.ID == gamemodeID);
        return mode != null;
    }
}
