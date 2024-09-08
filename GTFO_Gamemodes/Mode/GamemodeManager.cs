using Gamemodes.Mode.Tests;
using Gamemodes.Net;
using System;
using System.Collections.Generic;
using System.Linq;
using static Gamemodes.PatchManager;

namespace Gamemodes.Mode
{
    public class GamemodeManager
    {
        private static readonly HashSet<ModeInfo> _gamemodes = new();
        private static readonly HashSet<string> _gamemodeIds = new();

        public static IEnumerable<ModeInfo> LoadedModes => _gamemodes;
        public static IEnumerable<string> LoadedModeIds => _gamemodeIds;

        public static bool AllowDropWithVanillaPlayers => _currentMode?.GetType() == typeof(ModeGTFO);

        public static bool CurrentAllowsForcedTP => _currentMode?.Settings?.RequiresForcedTeleportation ?? false;

        internal static GamemodeBase CurrentMode => _currentMode;

        private static GamemodeBase _currentMode;

        private static ModeInfo _modeGTFO;

        internal static void Init()
        {
            NetworkingManager.DoSwitchModeReceived += OnSwitchModeReceived;
            GameEvents.OnGameDataInit += OnGameDataInit;

            _modeGTFO = RegisterMode<ModeGTFO>();
#if DEBUG
            RegisterMode<ModeTesting>();
            RegisterMode<ModeNoSleepers>();
#endif
        }

        private static void OnGameDataInit()
        {
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
                if(!_currentMode.Settings.AllowMidGameModeSwitch)
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

            var msg = $"Switching mode [{_currentMode?.ID ?? "None"}] => [{mode.ID}]";
            Plugin.L.LogDebug(msg);
            Plugin.PostLocalMessage(msg);

            _currentMode?.Disable();

            _currentMode = mode.Implementation;

            HandleSpecialRequirements(_currentMode.Settings);

            _currentMode.Enable();

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
            ApplyPatchGroup(PatchGroups.NO_FAIL, settings.PreventDefaultFailState);
            ApplyPatchGroup(PatchGroups.NO_RESPAWN, settings.PreventRespawnRoomsRespawning);
            ApplyPatchGroup(PatchGroups.NO_SLEEPING_ENEMIES, settings.PreventExpeditionEnemiesSpawning);
        }

        public static ModeInfo RegisterMode<T>() where T : GamemodeBase
        {
            var impl = Activator.CreateInstance<T>();

            var id = impl.ID;
            var displayName = impl.DisplayName;

            if (_gamemodeIds.Contains(id))
                throw new ArgumentException($"Gamemode \"{id}\" can't be registered twice!", nameof(id));

            var info = new ModeInfo(id, displayName, impl);

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
}
