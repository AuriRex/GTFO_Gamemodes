using HarmonyLib;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace Gamemodes;

public class PatchManager
{
    public static class PatchGroups
    {
        public const string NO_SLEEPING_ENEMIES = "NoSleepingEnemies";
        public const string NO_RESPAWN = "NoRespawn";
        public const string NO_FAIL = "NoFail";
        public const string REQUIRED = "Required";
        public const string DEBUG = "Debug";
        public const string NO_CHECKPOINTS = "NoCheckpoints";
        public const string NO_WORLDEVENTS = "NoWorldEvents";
        public const string FORCE_ARENA_DIM = "ForceArenaDimension";
        public const string NO_VOICE = "NoVoice";
        public const string USE_TEAM_VISIBILITY = "UseTeamVis";
        public const string NO_TERMINAL_COMMANDS = "NoTerminalCommands";
        public const string NO_BLOOD_DOORS = "NoBloodDoors";
        public const string NO_PLAYER_REVIVE = "NoPlayerRevive";
        public const string INF_PLAYER_AMMO = "InfPlayerAmmo";
        public const string INF_SENTRY_AMMO = "InfSentryAmmo";
        public const string PROXIMITY_VOICE = "ProximityVoiceComp";
        internal const string ACHIEVEMENT_PATCHES = "Achievement_Patch_Beeg_Lag_Yay";
    }

    private class PatchGroup
    {
        internal readonly string Name;
        internal readonly HashSet<Type> Types = new();
        internal readonly Harmony HarmonyInstance;
        internal bool IsPatched = false;

        public PatchGroup(string group)
        {
            Name = group;
            HarmonyInstance = new Harmony($"{Plugin.GUID}.{group}");
        }

        public bool Patch(bool patch)
        {
            if (IsPatched == patch)
                return false;

            if (patch)
            {
                Plugin.L.LogDebug($"Patching {nameof(PatchGroup)} \"{Name}\" ({Types.Count} Types) ...");
                foreach (var type in Types)
                {
                    HarmonyInstance.PatchAll(type);
                }
            }
            else
            {
                if (Name == PatchGroups.REQUIRED)
                    return false;

                if (Name == DEFAULT_PATCHGROUP && DefaultLock)
                    return false;

                Plugin.L.LogDebug($"Unpatching {nameof(PatchGroup)} \"{Name}\" ...");
                HarmonyInstance.UnpatchSelf();
            }

            IsPatched = patch;

            return true;
        }
    }

    internal const string DEFAULT_PATCHGROUP = "Default";
    public const string PATCHGROUP_FIELDNAME = "PatchGroup";

    internal static bool DefaultLock = true;

    private static readonly Dictionary<string, PatchGroup> _patchGroups = new();

    internal static void Init()
    {
        IterateTypes($"{nameof(Gamemodes)}.{nameof(Patches)}", Assembly.GetExecutingAssembly());

        ApplyPatchGroup(PatchGroups.REQUIRED, true);
        ApplyPatchGroup(DEFAULT_PATCHGROUP, true);
#if DEBUG
        ApplyPatchGroup(PatchGroups.DEBUG, true);
#endif
    }

    public static void IterateTypes(string nameSpacePrefix, Assembly asm)
    {
        if (string.IsNullOrWhiteSpace(nameSpacePrefix))
            throw new ArgumentException("Invalid parameter.", nameof(nameSpacePrefix));

        foreach (var type in AccessTools.GetTypesFromAssembly(asm))
        {
            if (!type.Namespace.StartsWith(nameSpacePrefix) || type.GetCustomAttribute<HarmonyPatch>() == null)
                continue;

            var FI_patchGroup = type.GetField(PATCHGROUP_FIELDNAME, AccessTools.all);

            string group = null;

            if (FI_patchGroup != null)
            {
                group = (string)FI_patchGroup.GetValue(null);
            }

            if (string.IsNullOrWhiteSpace(group))
            {
                group = DEFAULT_PATCHGROUP;
            }

            AddToGroup(group, type);
        }
    }

    private static void AddToGroup(string group, Type type)
    {
        if (!_patchGroups.TryGetValue(group, out var patchGroup))
        {
            patchGroup = new(group);
            _patchGroups.Add(group, patchGroup);
        }

        Plugin.L.LogDebug($"[{nameof(PatchManager)}] {group}: + {type.Name}");

        patchGroup.Types.Add(type);
    }

    public static bool ApplyPatchGroup(string group, bool patch)
    {
        if (!_patchGroups.TryGetValue(group, out var patchGroup))
        {
            return false;
        }

        return patchGroup.Patch(patch);
    }
}
