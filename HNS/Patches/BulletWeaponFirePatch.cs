using Gamemodes.Net;
using Gear;
using HarmonyLib;
using HNS.Core;
using HNS.Net;
using Player;
using UnityEngine;

namespace HNS.Patches;

[HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.Fire))]
public class BulletWeaponFirePatch
{
    public static void Postfix(BulletWeapon __instance)
    {
        NetworkingManager.GetPlayerInfo(__instance.Owner.Owner, out var info);

        // just in case I guess lol
        if (!info.IsLocal)
            return;

        if (!NetSessionManager.HasSession)
            return;
        
        if (!TeamHelper.IsSeeker(info.Team))
            return;

        if (__instance.AmmoType != AmmoType.Special)
            return;
        
        var camera = info.PlayerAgent.FPSCamera;

        var origin = camera.Position;
        Vector3 hitPoint = camera.Position + Weapon.s_weaponRayData.fireDir * 100f;
        
        
        if (Physics.Raycast(origin, Weapon.s_weaponRayData.fireDir, out var rayHit, 100f, LayerManager.MASK_WORLD))
        {
            hitPoint = rayHit.point;
        }
        
        NetSessionManager.SendEpicTracer(origin, hitPoint);
    }
}

// This works on pretty much any weapon (including sentry guns)
// For HNS we decided to only use tracers for the sniper
// So this code here is just here in case it's needed at some point :)
//
/*[HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.BulletHit))]
public class BulletWeapon__BulletHit__Patch
{
    public static void Postfix(Weapon.WeaponHitData weaponRayData)
    {
        if (!weaponRayData.owner.IsLocallyOwned)
            return;

        var fireDir = weaponRayData.fireDir;
        var distance = weaponRayData.rayHit.distance;
        var hitPoint = weaponRayData.rayHit.point;
        
        var origin = hitPoint + fireDir * -1 * distance;
        
        NetSessionManager.SendEpicTracer(origin, hitPoint);
    }
}*/