using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CellMenu;
using Gamemodes.Components;
using Gamemodes.Mode;
using Gamemodes.Net;
using Gamemodes.Patches;
using Il2CppInterop.Runtime.Injection;
using System.Reflection;
using System.Runtime.CompilerServices;

[assembly: AssemblyVersion(Gamemodes.Plugin.VERSION)]
[assembly: AssemblyFileVersion(Gamemodes.Plugin.VERSION)]
[assembly: AssemblyInformationalVersion(Gamemodes.Plugin.VERSION)]
[assembly: InternalsVisibleTo("GamemodesTests", AllInternalsVisible = true)]

namespace Gamemodes;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    public const string GUID = "dev.aurirex.gtfo.gamemodes";
    public const string NAME = "Gamemodes";
    public const string VERSION = "0.0.1";

    internal static ManualLogSource L;

    internal static PrimitiveVersion Version { get; private set; }

    public override void Load()
    {
        L = Log;

        Version = new PrimitiveVersion(VERSION);

        ClassInjector.RegisterTypeInIl2Cpp<PlayerToken>();

        PatchManager.Init();
        NetworkingManager.Init();
        GamemodeManager.Init();
    }

    public static void SendChatMessage(string msg)
    {
        var pcm = PlayerChatManager.Current;

        if (pcm == null)
            return;

        var prevMsg = pcm.m_currentValue;
        pcm.m_currentValue = msg;
        pcm.PostMessage();
        pcm.m_currentValue = prevMsg;
    }

    public static void PostLocalMessage(string msg, eGameEventChatLogType type = eGameEventChatLogType.GameEvent)
    {
        PostLocalMessageTo(CM_PageLoadout.Current?.m_gameEventLog, msg, type);
        PostLocalMessageTo(CM_PageMap.Current?.m_gameEventLog, msg, type);
        PostLocalMessageTo(GuiManager.PlayerLayer?.m_gameEventLog, msg, type);
    }

    private static void PostLocalMessageTo(PUI_GameEventLog log, string msg, eGameEventChatLogType type = eGameEventChatLogType.GameEvent)
    {
        if (log == null)
            return;
        log.AddLogItem(msg, type);
    }
}