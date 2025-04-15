using HarmonyLib;
// ReSharper disable InconsistentNaming

namespace HNS.Patches;

[HarmonyPatch(typeof(LocalPlayerAgentSettings), nameof(LocalPlayerAgentSettings.UpdateBlendTowardsTargetFogSetting))]
public class LocalPlayerAgentSettings__UpdateBlendTowardsTargetFogSetting__Patch
{
    private static float _infection;
    private static float _Target_Infection;
    private static float _Target_FogDensity;
    private static float _Target_DensityHeightMaxBoost;
    public static void Prefix(LocalPlayerAgentSettings __instance)
    {
        //Plugin.L.LogWarning("LocalPlayerAgentSettings__UpdateBlendTowardsTargetFogSetting__Patch  PREFIX");
        var fogSettings = __instance.m_fogSettings;
        _infection = fogSettings.Infection;
        fogSettings.Infection = 0;

        var db = __instance.m_targetFogSettings;
        _Target_Infection = db.Infection;
        _Target_FogDensity = db.FogDensity;
        _Target_DensityHeightMaxBoost = db.DensityHeightMaxBoost;
        
        db.Infection = 0f;
        
        // Almost completely clears fog
        db.FogDensity = _Target_FogDensity; //0.00005f;
        db.DensityHeightMaxBoost = _Target_FogDensity * 2f; //0.0001f;
    }

    public static void Postfix(LocalPlayerAgentSettings __instance)
    {
        // Plugin.L.LogWarning("LocalPlayerAgentSettings__UpdateBlendTowardsTargetFogSetting__Patch  POSTFIX");
        var fogSettings = __instance.m_fogSettings;
        fogSettings.Infection = _infection;
        __instance.m_fogSettings = fogSettings;
        
        var db = __instance.m_targetFogSettings;
        db.Infection = _Target_Infection;
        db.FogDensity = _Target_FogDensity;
        db.DensityHeightMaxBoost = _Target_DensityHeightMaxBoost;
    }
}