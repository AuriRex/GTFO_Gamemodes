using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(DramaManager), nameof(DramaManager.Update))]
public class DramaManagerPatch
{
    public static bool Prefix()
    {
        return false;
    }
}

[HarmonyPatch(typeof(PlayerStamina), nameof(PlayerStamina.UseStamina))]
public class StaminaPatch
{
    public static bool Prefix()
    {
        return false;
    }
}