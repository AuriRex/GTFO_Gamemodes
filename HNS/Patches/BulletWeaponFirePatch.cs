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
        
        if (info.Team != (int)GMTeam.Seekers)
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