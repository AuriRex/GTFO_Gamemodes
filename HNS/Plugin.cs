using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using HNS.Core;
using System.Reflection;

[assembly: AssemblyVersion(HNS.Plugin.VERSION)]
[assembly: AssemblyFileVersion(HNS.Plugin.VERSION)]
[assembly: AssemblyInformationalVersion(HNS.Plugin.VERSION)]

namespace HNS;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(Gamemodes.Plugin.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    public const string GUID = "dev.aurirex.gtfo.gamemode.hns";
    public const string NAME = "Hide and Seek";
    public const string VERSION = "0.5.1";

    internal static ManualLogSource L;

    public override void Load()
    {
        L = Log;

        Gamemodes.Core.GamemodeManager.RegisterMode<HideAndSeekMode>();
    }
}