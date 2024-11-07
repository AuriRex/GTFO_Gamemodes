using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace Gamemodes.Patches.Required;

[HarmonyPatch]
internal static class AchievementPatches
{
    public static readonly string PatchGroup = PatchManager.PatchGroups.REQUIRED;
    
    public static IEnumerable<MethodBase> TargetMethods()
    {
        var types = AccessTools.GetTypesFromAssembly(typeof(AchievementTask).Assembly);

        foreach (var type in types)
        {
            if (type == null)
                continue;

            if (!type.IsAssignableTo(typeof(AchievementTask)))
                continue;

            if (type == typeof(AchievementTask))
                continue;
            
            //Plugin.L.LogError($"{type.FullName}");
            foreach (var method in AccessTools.GetDeclaredMethods(type))
            {
                if (method.Name.StartsWith("get_") || method.Name.StartsWith("set_"))
                    continue;

                if (method.Name == nameof(AchievementTask.OnSetup))
                    continue;
                
                //Plugin.L.LogWarning($"{type.FullName}.{method.Name}");
                yield return method;
            }
        }

        var other = new MethodBase[]
        {
            AccessTools.Method(typeof(AchievementManager), nameof(AchievementManager.Update)),
            AccessTools.Method(typeof(AchievementManager), nameof(AchievementManager.OnLevelCleanup)),
            AccessTools.Method(typeof(AchievementManager), nameof(AchievementManager.OnStateChange)),
            AccessTools.Method(typeof(AchievementManager), nameof(AchievementManager.OnAchievementStateChanged)),
            AccessTools.Method(typeof(AchievementManager), nameof(AchievementManager.OnLevelGenDone)),
            AccessTools.Method(typeof(AchievementManager), nameof(AchievementManager.AttemptInteract)),
            //AccessTools.Method(typeof(Achievement_CompleteNoDownedTeam), nameof(Achievement_CompleteNoDownedTeam.OnPlayerDowned)),
        };

        foreach (var method in other)
        {
            yield return method;
        }
    }
    
    // Running the original methods here just crashes the game
    // So once it's patched we can't toggle it easily without
    // Having to wait like 4 seconds for all the methods to patch/unpatch
    public static bool Prefix() => false;
}
