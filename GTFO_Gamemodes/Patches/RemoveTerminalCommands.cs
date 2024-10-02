using GameData;
using HarmonyLib;
using Il2CppInterop.Runtime.InteropTypes.Arrays;
using LevelGeneration;
using Localization;
using System;
using System.Collections.Generic;
using System.Reflection;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

//public void AddCommand(TERM_Command cmd, string cmdString, LocalizedText helpString, TERM_CommandRule cmdRule, List<WardenObjectiveEventData> cmdEvents, List<TerminalOutput> postTerminalOutput)
[HarmonyPatch(typeof(LG_ComputerTerminalCommandInterpreter), nameof(LG_ComputerTerminalCommandInterpreter.AddCommand), new Type[]
    { typeof(TERM_Command), typeof(string), typeof(LocalizedText), typeof(TERM_CommandRule), typeof( Il2CppSystem.Collections.Generic.List<WardenObjectiveEventData>), typeof( Il2CppSystem.Collections.Generic.List<TerminalOutput>) })]
internal class RemoveTerminalCommands
{
    public static readonly string PatchGroup = PatchGroups.NO_TERMINAL_COMMANDS;

    public static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch]
internal static class RemoveTerminalCommands_Cancel
{
    public static readonly string PatchGroup = PatchGroups.NO_TERMINAL_COMMANDS;

    [HarmonyTargetMethods]
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var term = typeof(LG_ComputerTerminal);
        return new MethodBase[]
        {
            term.GetMethod(nameof(LG_ComputerTerminal.SetupAsWardenObjectiveCorruptedTerminalUplink)),
            term.GetMethod(nameof(LG_ComputerTerminal.SetupAsWardenObjectiveTerminalUplink)),
            term.GetMethod(nameof(LG_ComputerTerminal.SetupAsWardenObjectiveTimedTerminalSequence)),
            term.GetMethod(nameof(LG_ComputerTerminal.SetupAsWardenObjectiveSpecialCommand)),
            term.GetMethod(nameof(LG_ComputerTerminal.SetupAsWardenObjectiveGatherTerminal)),
            term.GetMethod(nameof(LG_ComputerTerminal.LockWithPassword), AccessTools.all, new Type[] { typeof(string), typeof(Il2CppStringArray) }),
        };
    }

    public static bool Prefix()
    {
        return false;
    }
}
