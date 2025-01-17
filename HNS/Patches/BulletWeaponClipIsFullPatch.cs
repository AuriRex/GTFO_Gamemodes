using Gear;
using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(BulletWeapon), nameof(BulletWeapon.ClipIsFull))]
public class BulletWeapon_ClipFull_Patch
{
    public static bool Prefix(BulletWeapon __instance, ref bool __result)
    {
        // >= instead of ==
        __result = __instance.m_clip >= __instance.ClipSize;
        return false;
    }
}