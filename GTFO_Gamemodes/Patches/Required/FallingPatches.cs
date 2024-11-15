using Gear;
using HarmonyLib;
using SNetwork;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(GlueGunProjectile), nameof(GlueGunProjectile.Update))]
internal static class FallingPatch_Glue
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(GlueGunProjectile __instance)
    {
        if (__instance.transform.position.y > -1000)
        {
            return;
        }

        // Null-checks its gameobject, so should be fine to call this locally as well
        __instance.SyncDestroy();
        
        if (!SNet.IsMaster)
            return;
        
        // Calls SyncDestroy on self and clients
        ProjectileManager.WantToDestroyGlue(__instance.SyncID);
    }
}

[HarmonyPatch(typeof(ShellCasing), nameof(ShellCasing.Awake))]
internal static class FallingPatch_Shelling
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(ShellCasing __instance)
    {
        if (__instance.m_bounces < 0)
            return;
        
        if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            return;
        
        __instance.m_bounces = -20;
        UnityEngine.Object.Destroy(__instance.gameObject);
    }
}

[HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.InitializeMagDropPool))]
internal static class BulletWeapon_InitializeMagDropPool_Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    // Populates BulletWeapon.m_magDropPool, but it's null checked in FixedUpdate so we can just not init it
    // Only called once on gear spawn
    public static bool Prefix(BulletWeapon __instance)
    {
        return false;
    }
}