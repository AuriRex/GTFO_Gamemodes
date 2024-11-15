using Gamemodes.Core;
using HarmonyLib;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.Setup))]
internal static class Mine_Setup_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(MineDeployerInstance __instance)
    {
        ToolInstanceCaches.MineCache.Register(__instance);
    }
}

[HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.OnDestroy))]
internal static class Mine_Destroy_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(MineDeployerInstance __instance)
    {
        ToolInstanceCaches.MineCache.Deregister(__instance);
    }
}

[HarmonyPatch(typeof(GlueGunProjectile), nameof(GlueGunProjectile.Awake))]
internal static class Glue_Setup_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(GlueGunProjectile __instance)
    {
        ToolInstanceCaches.GlueCache.Register(__instance);
    }
}

[HarmonyPatch(typeof(GlueGunProjectile), nameof(GlueGunProjectile.SyncDestroy))]
internal static class Glue_Destroy_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(GlueGunProjectile __instance)
    {
        ToolInstanceCaches.GlueCache.Deregister(__instance);
    }
}

[HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.Setup))]
internal static class Sentry_Setup_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(SentryGunInstance __instance)
    {
        ToolInstanceCaches.SentryCache.Register(__instance);
    }
}

[HarmonyPatch(typeof(SentryGunInstance), nameof(SentryGunInstance.OnDestroy))]
internal static class Sentry_Destroy_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(SentryGunInstance __instance)
    {
        ToolInstanceCaches.SentryCache.Deregister(__instance);
    }
}