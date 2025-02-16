using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(LocalPlayerAgentSettings), nameof(LocalPlayerAgentSettings.UpdateBlendTowardsTargetFogSetting))]
public class LocalPlayerAgentSettings__UpdateBlendTowardsTargetFogSetting__Patch
{
    private static float _infection;
    private static float _infectionTarget;
    public static void Prefix(LocalPlayerAgentSettings __instance)
    {
        //Plugin.L.LogWarning("LocalPlayerAgentSettings__UpdateBlendTowardsTargetFogSetting__Patch  PREFIX");
        var fogSettings = __instance.m_fogSettings;
        _infection = fogSettings.Infection;
        fogSettings.Infection = 0;

        var db = __instance.m_targetFogSettings;
        _infectionTarget = db.Infection;
        db.Infection = 0f;
    }

    public static void Postfix(LocalPlayerAgentSettings __instance)
    {
        // Plugin.L.LogWarning("LocalPlayerAgentSettings__UpdateBlendTowardsTargetFogSetting__Patch  POSTFIX");
        var fogSettings = __instance.m_fogSettings;
        fogSettings.Infection = _infection;
        __instance.m_fogSettings = fogSettings;
        
        var db = __instance.m_targetFogSettings;
        db.Infection = _infectionTarget;
    }
}