using BepInEx;
using BepInEx.Logging;
using BepInEx.Unity.IL2CPP;
using System.Reflection;
using ExtraModes.GeoGuesser;
using ExtraModes.TheDarkness;

[assembly: AssemblyVersion(ExtraModes.Plugin.VERSION)]
[assembly: AssemblyFileVersion(ExtraModes.Plugin.VERSION)]
[assembly: AssemblyInformationalVersion(ExtraModes.Plugin.VERSION)]

namespace ExtraModes;

[BepInPlugin(GUID, NAME, VERSION)]
[BepInDependency("dev.gtfomodding.gtfo-api", BepInDependency.DependencyFlags.HardDependency)]
[BepInDependency(Gamemodes.Plugin.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    public const string GUID = "dev.aurirex.gtfo.gamemode.extramodes";
    public const string NAME = "Extra Modes";
    public const string VERSION = "0.0.1";

    internal static ManualLogSource L;

    public override void Load()
    {
        L = Log;

        Gamemodes.Core.GamemodeManager.RegisterMode<TheDarkness.TheDarkness>();
        Gamemodes.Core.GamemodeManager.RegisterMode<TheDarknessLevelTwo>();
        Gamemodes.Core.GamemodeManager.RegisterMode<TheDarknessLevelThree>();
                
        Gamemodes.Core.GamemodeManager.RegisterMode<GGMode>();
    }
}