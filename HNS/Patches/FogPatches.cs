using System;
using HarmonyLib;
// ReSharper disable InconsistentNaming

namespace HNS.Patches;

[HarmonyPatch(typeof(LocalPlayerAgentSettings), nameof(LocalPlayerAgentSettings.UpdateBlendTowardsTargetFogSetting))]
public class LocalPlayerAgentSettings__UpdateBlendTowardsTargetFogSetting__Patch
{
    private const float FOG_DENSITY_CLAMP_MAX = 0.001f;
    
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
        
        // R5E1 - 0.007 - fog completely envelopes everything
        // R8B4 - 0.0004

        // Special case mostly for R5E1's inverted fog
        var invert = _Target_DensityHeightMaxBoost < _Target_FogDensity;
        
        var fogDensityLow = Math.Min(_Target_DensityHeightMaxBoost, _Target_FogDensity);
        
        var fogClamped = Math.Clamp(fogDensityLow, 0, FOG_DENSITY_CLAMP_MAX);
        
        db.FogDensity = fogClamped * (invert ? 2f : 1f);
        db.DensityHeightMaxBoost = fogClamped * (invert ? 1f : 2f);
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