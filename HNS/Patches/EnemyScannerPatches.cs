using Gamemodes.Extensions;
using Gear;
using HarmonyLib;
using HNS.Components;

namespace HNS.Patches;

[HarmonyPatch(typeof(EnemyScanner), nameof(EnemyScanner.OnWield))]
public class EnemyScanner__OnWield__Patch
{
    public static void Postfix(EnemyScanner __instance)
    {
        __instance.gameObject.GetOrAddComponent<PlayerTrackerController>();
    }
}

[HarmonyPatch(typeof(EnemyScanner), nameof(EnemyScanner.Update))]
public class EnemyScanner__Update__Patch
{
    public static bool Prefix(EnemyScanner __instance)
    {
        __instance.Sound.UpdatePosition(__instance.transform.position);
        return false;
    }
}