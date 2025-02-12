﻿using Gamemodes.Core;
using Gamemodes.Net;
using HarmonyLib;
using Player;
using SNetwork;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using CellMenu;
using Gamemodes.UI.Menu;
using Gear;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyWrapSafe]
[HarmonyPatch(typeof(PlayerChatManager), nameof(PlayerChatManager.PostMessage))]
internal static class ChatCommandsHandler
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;

    private static readonly Dictionary<string, MethodInfo> _commands = new();

    internal static void AddCommand(string command, Func<string[], string> func)
    {
        AddCommand(command, func.Method);
    }

    internal static void AddCommand(string command, MethodInfo methodInfo)
    {
        if (_commands.ContainsKey(command))
        {
            throw new ArgumentException($"Command \"{command}\" already registered!", nameof(command));
        }

        _commands.Add(command, methodInfo);
    }

    private static void SetupCommands()
    {
        var methods = typeof(ChatCommandsHandler).GetMethods().Where(mi => mi.Name != nameof(Prefix) && mi.Name != nameof(AddCommand));

        foreach (var mi in methods)
        {
            var command = mi.Name.ToLower();
            if (_commands.ContainsKey(command))
            {
                Plugin.L.LogDebug($"[{nameof(ChatCommandsHandler)}] Duplicate method?? \"{command}\"");
                continue;
            }

            _commands.Add(command, mi);
        }
    }

    private static bool SkipOG()
    {
        PlayerChatManager.Current.m_currentValue = string.Empty;
        PlayerChatManager.Current.ExitChatMode();
        return false;
    }

    private static bool _once = true;

    public static bool Prefix(PlayerChatManager __instance)
    {
        var message = __instance.m_currentValue;

        if (message.Length > 2 && message.StartsWith("/"))
        {
            if (_once)
            {
                SetupCommands();
                _once = false;
            }

            message = message.Substring(1);

            var split = message.Split(" ", StringSplitOptions.RemoveEmptyEntries);

            var command = split[0].ToLower();
            string[] args = Array.Empty<string>();

            if (split.Length > 1)
            {
                args = split.Skip(1).ToArray();
            }

            if (!_commands.TryGetValue(command, out var commandMI))
            {
                var customCommands = GamemodeManager.CurrentMode?.ChatCommands;

                if (customCommands == null || !customCommands.Get(command, out commandMI))
                {
                    Plugin.PostLocalMessage($"Unknown command \"{command}\".", eGameEventChatLogType.Alert);
                    return SkipOG();
                }
            }

            try
            {
                string result = (string)commandMI.Invoke(null, new object[] { args });

                if (!string.IsNullOrWhiteSpace(result))
                    Plugin.PostLocalMessage(result, eGameEventChatLogType.GameEvent);
            }
            catch (Exception ex)
            {
                if (ex is TargetInvocationException tie)
                {
                    ex = tie.InnerException;
                }

                var exmsg = $"Command \"{command}\" threw a {ex.GetType().Name}!";

                Plugin.PostLocalMessage(exmsg, eGameEventChatLogType.Alert);
                Plugin.PostLocalMessage(ex.Message, eGameEventChatLogType.Alert);

                Plugin.L.LogError(exmsg);
                Plugin.L.LogError(ex.Message);
                Plugin.L.LogWarning($"StackTrace:\n{ex.StackTrace}");
            }

            return SkipOG();
        }

        return true;
    }

    #region commands

    public static string CurrentMode(string[] args)
    {
        return $"Current Gamemode: {GamemodeManager.CurrentMode.ID}: {GamemodeManager.CurrentMode.DisplayName}";
    }

    public static string ListModes(string[] args)
    {
        return string.Join(", ", GamemodeManager.LoadedModeIds);
    }

    public static string SwitchMode(string[] args)
    {
        var mode = args[0];

        var previousMode = GamemodeManager.CurrentMode.ID;

        NetworkingManager.SendSwitchModeAll(mode);

        return $"Trying to switch mode from [{previousMode}] to [{mode}].";
    }

    public static string NoFlash(string[] args) => PhotoSensitivity(args);
    public static string AntiFlash(string[] args) => PhotoSensitivity(args);

    public static string PhotoSensitivity(string[] args)
    {
        GamemodeManager.PhotoSensitivityMode = !GamemodeManager.PhotoSensitivityMode;

        Plugin.PostLocalMessage($"<color=orange>{nameof(GamemodeManager.PhotoSensitivityMode)}: {(GamemodeManager.PhotoSensitivityMode ? "<#0F0>Enabled</color>" : "<#F00>Disabled</color>")}!</color>");
        
        return "<color=white><i>(This value is currently not saved in between game sessions!)</i></color>";
    }
    
#if DEBUG
    public static string Test(string[] args)
    {
        return "Hi, this is a test! :D";
    }

    public static string Echo(string[] args)
    {
        return string.Join(' ', args);
    }

    public static string Throw(string[] args)
    {
        throw new Exception("This is supposed to happen. :)");
    }

    public static string PushForceDebug(string[] args) => PushForceMulti(args);
    public static string PushForceMulti(string[] args)
    {
        float? oldPushMulti = null;
        float? oldSlidePushMulti = null;
        if (args.Length > 0 && float.TryParse(args[0], out var value))
        {
            oldPushMulti = PushForcePatch.PushForceMultiplier;
            PushForcePatch.PushForceMultiplier = value;

            if (oldPushMulti == value)
                oldPushMulti = null;
        }
        
        if (args.Length > 1 && float.TryParse(args[1], out var slidePush))
        {
            oldSlidePushMulti = PushForcePatch.SlidePushForceMultiplier;
            PushForcePatch.SlidePushForceMultiplier = slidePush;

            if (oldSlidePushMulti == slidePush)
                oldSlidePushMulti = null;
        }

        var msg = $"PushForceMulti: {(oldPushMulti.HasValue ? $"{oldPushMulti.Value} -> " : string.Empty)}{PushForcePatch.PushForceMultiplier}";
        msg += $" | Sl: {(oldSlidePushMulti.HasValue ? $"{oldSlidePushMulti.Value} -> " : string.Empty)}{PushForcePatch.SlidePushForceMultiplier}";
        
        return msg;
    }

    public static string SpawnItem(string[] args)
    {
        if (!SNet.IsMaster)
            return "Master only!";

        if (GamemodeManager.IsVanilla)
            return "No.";
        
        if (args.Length < 1 || !uint.TryParse(args[0], out uint itemId))
            return "Could not parse item id from arguments.";
        
        if (args.Length < 2 || !float.TryParse(args[1], out var ammoMultiplier))
        {
            ammoMultiplier = 1f;
        }
        
        if (!PlayerManager.TryGetLocalPlayerAgent(out var player))
            return "Not in level. / No Agent.";
        
        NetworkingManager.SendSpawnItemInLevel(player.CourseNode, player.Position, itemId, ammoMultiplier);
        return "SpawnItem sent.";
    }

    public static string DoorTest(string[] args)
    {
        Utils.LocallyResetAllWeakDoors();
        
        return "Door Test complete.";
    }

    public static string GearTest(string[] args)
    {
        CoroutineManager.StartCoroutine(GearTestButOneFrameDelay().WrapToIl2Cpp());
        
        return "ok";
    }

    private static IEnumerator GearTestButOneFrameDelay()
    {
        yield return null;
        
        var items = GearManager.GetAllGearForSlot(InventorySlot.GearClass).ToArray();

        var selector = new CustomGearSelector(items, InventorySlot.GearClass);
        
        selector.Show();
    }
#endif

    public static string Sync(string[] args) => ManualJoin(args);
    
    public static string ManualJoin(string[] args)
    {
        NetworkingManager.SendJoinInfo();

        return "Join info sent.";
    }

    public static string SetTeam(string[] args)
    {
        if (!SNet.IsMaster)
            return "Master only.";

        if (GamemodeManager.IsVanilla)
            return "No.";
        
        var team = args[0];

        if (int.TryParse(team, out var teamInt))
        {
            SNet_Player player = SNet.LocalPlayer;

            if (args.Length > 1 && int.TryParse(args[1], out var playerIndex) && PlayerManager.TryGetPlayerAgent(ref playerIndex, out var playerAgent))
            {
                player = playerAgent.Owner;
            }

            NetworkingManager.AssignTeam(player, teamInt);

            return $"Assigned \"{player.NickName}\" to team {teamInt}";
        }

        return "Oh no, it borky (couldn't parse int) :c";
    }

    public static string TP(string[] args)
    {
        if (!SNet.IsMaster)
            return "Master only.";

        if (GamemodeManager.IsVanilla)
            return "No.";
        
        if (args.Length == 0)
            throw new ArgumentException("No player to TP to specified.", nameof(args));

        var lp = PlayerManager.GetLocalPlayerAgent();

        if (!NetworkingManager.InLevel || lp == null)
            throw new InvalidOperationException("Not in level or local player agent is null!");

        PlayerAgent player = null;

        if (int.TryParse(args[0], out var slot))
        {
            PlayerManager.TryGetPlayerAgent(ref slot, out player);
        }
        else
        {
            foreach (var pw in PlayerManager.PlayerAgentsInLevel)
            {
                if (pw.Owner.NickName.Contains(args[0]))
                {
                    player = pw;
                    break;
                }
            }
        }

        if (player == null)
            throw new ArgumentException("No player to TP to found.", nameof(args));

        NetworkingManager.SendForceTeleport(lp.Owner, player.Position, player.TargetLookDir, player.DimensionIndex, PlayerAgent.WarpOptions.ShowScreenEffectForLocal | PlayerAgent.WarpOptions.PlaySounds | PlayerAgent.WarpOptions.WithoutBots);

        return $"Trying to TP to {player.Owner.NickName} ...";
    }

    public static string TPAll(string[] args)
    {
        if (!SNet.IsMaster)
            return "Master only.";

        if (GamemodeManager.IsVanilla)
            return "No.";
        
        var lp = PlayerManager.GetLocalPlayerAgent();

        if (!NetworkingManager.InLevel || lp == null)
            throw new InvalidOperationException("Not in level or local player agent is null!");

        foreach (var pw in NetworkingManager.AllValidPlayers)
        {
            if (pw.NetPlayer.IsLocal)
                continue;

            NetworkingManager.SendForceTeleport(pw.NetPlayer, lp.Position, lp.TargetLookDir, lp.DimensionIndex, PlayerAgent.WarpOptions.ShowScreenEffectForLocal | PlayerAgent.WarpOptions.PlaySounds | PlayerAgent.WarpOptions.WithoutBots);
        }

        return "Tried to teleport all players to you.";
    }
    #endregion
}
