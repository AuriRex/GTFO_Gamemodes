using HarmonyLib;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(SentryGunInstance_Firing_Bullets), nameof(SentryGunInstance_Firing_Bullets.UpdateAmmo))]
internal class SentryPatch
{
    public static readonly string PatchGroup = PatchGroups.INF_SENTRY_AMMO;

    public static bool Prefix(SentryGunInstance_Firing_Bullets __instance)
    {
        __instance.m_core.Ammo = __instance.m_core.AmmoMaxCap; // shrug
        return false;
    }
}
