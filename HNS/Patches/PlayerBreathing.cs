using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(PlayerBreathing), nameof(PlayerBreathing.Setup))]
internal static class PlayerBreathing__Setup__Patch
{
    public static void Postfix(PlayerBreathing __instance)
    {
        __instance.enabled = false;
    }
}