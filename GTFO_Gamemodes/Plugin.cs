using System;
using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using CellMenu;
using Gamemodes.Components;
using Gamemodes.Core;
using Gamemodes.Net;
using Gamemodes.Patches;
using Il2CppInterop.Runtime.Injection;
using System.Reflection;
using System.Runtime.CompilerServices;
using Gamemodes.Components.L2;
using Gamemodes.Core.Voice;

[assembly: AssemblyVersion(Gamemodes.Plugin.VERSION)]
[assembly: AssemblyFileVersion(Gamemodes.Plugin.VERSION)]
[assembly: AssemblyInformationalVersion(Gamemodes.Plugin.VERSION)]
[assembly: InternalsVisibleTo("GamemodesTests", AllInternalsVisible = true)]

namespace Gamemodes;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(GUID_DIMENSIONMAPS, BepInDependency.DependencyFlags.SoftDependency)]
public class Plugin : BasePlugin
{
    public const string GUID_DIMENSIONMAPS = "dev.aurirex.gtfo.dimensionmaps";
    
    public const string GUID = "dev.aurirex.gtfo.gamemodes";
    public const string NAME = "Gamemodes";
    public const string VERSION = "0.5.1";

    internal static ManualLogSource L;

    internal static PrimitiveVersion Version { get; private set; }

    internal static MethodInfo MI_dimensionMaps_revealMapTexture;
    
    public override void Load()
    {
        L = Log;

        Version = new PrimitiveVersion(VERSION);

        ClassInjector.RegisterTypeInIl2Cpp<PlayerToken>();
        ClassInjector.RegisterTypeInIl2Cpp<TimerHUD>();
        ClassInjector.RegisterTypeInIl2Cpp<PUI_TeamDisplay>();
        ClassInjector.RegisterTypeInIl2Cpp<ProximityVoice>();
        ClassInjector.RegisterTypeInIl2Cpp<NodeDistance>();
        
        ClassInjector.RegisterTypeInIl2Cpp<FlashBlinder>();
        ClassInjector.RegisterTypeInIl2Cpp<FlashGrenadeInstance>();

        PatchManager.Init();
        NetworkingManager.Init();
        GamemodeManager.Init();
        PlayerVoiceManager.Init();

        GameEvents.OnGameDataInit += PrefabManager.Init;
        GameEvents.PreItemPrefabsSetup += PrefabManager.PreItemLoading;
        GameEvents.OnItemPrefabsSetup += PrefabManager.OnAssetLoaded;

        if (IL2CPPChainloader.Instance.Plugins.TryGetValue(GUID_DIMENSIONMAPS, out var pluginInfo))
        {
            MI_dimensionMaps_revealMapTexture = pluginInfo.Instance.GetType().Assembly.GetType("DimensionMaps.Core.CMapDetails")
                ?.GetProperty("RevealMapTexture")
                ?.GetSetMethod();
            
            if (MI_dimensionMaps_revealMapTexture != null)
            {
                Log.LogInfo($"Found 'CMapDetails.RevealMapTexture' SetMethod MethodInfo!");
            }
            else
            {
                Log.LogWarning($"Did NOT find 'CMapDetails.RevealMapTexture' SetMethod MethodInfo!");
            }
        }
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

    internal static void LogException(Exception ex, string info, bool printInChat = false)
    {
        var msg = $"{ex.GetType().Name} thrown in {info}.";
        if (printInChat)
            PostLocalMessage(msg, eGameEventChatLogType.Alert);
        L.LogError(msg);
        L.LogError(ex.Message);
        L.LogWarning("Stacktrace:\n"+ex.StackTrace);
    }
}