using CellMenu;
using Gamemodes.Mode;
using Gamemodes.Net;
using HarmonyLib;
using Player;
using SNetwork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches
{
    [HarmonyWrapSafe]
    [HarmonyPatch(typeof(PlayerChatManager), nameof(PlayerChatManager.PostMessage))]
    public static class PlayerChatManager_PostMessage_Patch
    {
        public static readonly string PatchGroup = PatchGroups.DEBUG;

        private static Dictionary<string, MethodInfo> _commands = null;

        private static void SetupCommands()
        {
            if (_commands != null)
                return;

            _commands = new();

            var methods = typeof(PlayerChatManager_PostMessage_Patch).GetMethods().Where(mi => mi.Name != nameof(Prefix));

            foreach(var mi in methods)
            {
                var command = mi.Name.ToLower();
                if(_commands.ContainsKey(command))
                {
                    Plugin.L.LogDebug($"[{nameof(PlayerChatManager_PostMessage_Patch)}] Duplicate method?? \"{command}\"");
                    continue;
                }

                _commands.Add(command, mi);
            }
        }

        private static bool SkipOG()
        {
            PlayerChatManager.Current.m_currentValue = string.Empty;
            return false;
        }

        public static bool Prefix(PlayerChatManager __instance)
        {
            var message = __instance.m_currentValue;

            if (message.Length > 2 && message.StartsWith("/"))
            {
                SetupCommands();

                message = message.Substring(1);

                var split = message.Split(" ", StringSplitOptions.RemoveEmptyEntries);

                var command = split[0].ToLower();
                string[] args = Array.Empty<string>();

                if(split.Length > 1)
                {
                    args = split.Skip(1).ToArray();
                }

                if(!_commands.TryGetValue(command, out var commandMI))
                {
                    Plugin.PostLocalMessage($"Unknown command \"{command}\".", eGameEventChatLogType.Alert);
                    return SkipOG();
                }

                try
                {
                    string result = (string) commandMI.Invoke(null, new object[] { args });

                    if(!string.IsNullOrWhiteSpace(result))
                        Plugin.PostLocalMessage(result, eGameEventChatLogType.GameEvent);
                }
                catch(Exception ex)
                {
                    if(ex is TargetInvocationException tie)
                    {
                        ex = tie.InnerException;
                    }

                    Plugin.PostLocalMessage($"Command \"{command}\" threw a {ex.GetType().Name}!", eGameEventChatLogType.Alert);
                    Plugin.PostLocalMessage(ex.Message, eGameEventChatLogType.Alert);
                }

                return SkipOG();
            }

            return true;
        }

        #region commands
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

        public static string CurrentMode(string[] args)
        {
            return $"Current Gamemode: {GamemodeManager.CurrentMode.ID}: {GamemodeManager.CurrentMode.DisplayName}";
        }

        public static string ListModes(string[] args)
        {
            return string.Join(", ", GamemodeManager.LoadedModeIds);
        }

        public static string ManualJoin(string[] args)
        {

            NetworkingManager.SendJoinInfo();

            return "";
        }

        public static string SwitchMode(string[] args)
        {
            var mode = args[0];

            var previousMode = GamemodeManager.CurrentMode.ID;

            NetworkingManager.SendSwitchModeAll(mode);

            return $"Trying to switch mode from [{previousMode}] to [{mode}].";
        }

        public static string TP(string[] args)
        {
            if (args.Length == 0)
                throw new ArgumentException("No player to TP to specified.", nameof(args));

            var lp = PlayerManager.GetLocalPlayerAgent();

            if (!NetworkingManager.InLevel || lp == null)
                throw new InvalidOperationException("Not in level or local player agent is null!");

            PlayerAgent player = null;

            if(int.TryParse(args[0], out var slot))
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
            var lp = PlayerManager.GetLocalPlayerAgent();

            if (!NetworkingManager.InLevel || lp == null)
                throw new InvalidOperationException("Not in level or local player agent is null!");

            foreach(var pw in NetworkingManager.AllValidPlayers)
            {
                if (pw.NetPlayer.IsLocal)
                    continue;

                NetworkingManager.SendForceTeleport(pw.NetPlayer, lp.Position, lp.TargetLookDir, lp.DimensionIndex, PlayerAgent.WarpOptions.ShowScreenEffectForLocal | PlayerAgent.WarpOptions.PlaySounds | PlayerAgent.WarpOptions.WithoutBots);
            }

            return "Tried to teleport all players to you.";
        }
        #endregion
    }
}
