using HarmonyLib;
using SNetwork;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(GlueGunProjectile), nameof(GlueGunProjectile.Update))]
public class FallingPatch_Glue
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
public class FallingPatch_Shelling
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