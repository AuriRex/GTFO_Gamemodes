// due to some yet undiscovered whatever (maybe the shader is hard capped to 40? maybe not?)
// the two patch classes below are not enough to raise the light limit
// all you're getting is a bunch of flickering lights due to
// what I can only assume to be the game rendering shadows on top of each other

/*
using HarmonyLib;
using UnityEngine;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(CL_ShadowAtlas), nameof(CL_ShadowAtlas.Reset))]
public class LightExpansionTest
{
    public static readonly string PatchGroup = PatchManager.PatchGroups.REQUIRED;
    
    public const int DEFAULT_DYNAMIC_LIGHT_ID_COUNT = 8; // 4 Players + 4 sentries? idk
    public const int DEFAULT_STATIC_LIGHT_ID_COUNT = 32;
    
    public static int DYNAMIC_LIGHT_ID_COUNT_OVERRIDE = 32;
    public static int STATIC_LIGHT_ID_COUNT_OVERRIDE = 32;
    
    public static void Postfix()
    {
        if (DYNAMIC_LIGHT_ID_COUNT_OVERRIDE <= DEFAULT_DYNAMIC_LIGHT_ID_COUNT)
            return;

        if (STATIC_LIGHT_ID_COUNT_OVERRIDE < DEFAULT_STATIC_LIGHT_ID_COUNT)
            return;
        
        Plugin.L.LogWarning($"{nameof(LightExpansionTest)} Postfix running! :D");
        
        CL_ShadowAtlas.s_dynamicIDPool.Clear();
        CL_ShadowAtlas.s_staticIDPool.Clear();
        
        for (int j = 0; j < DYNAMIC_LIGHT_ID_COUNT_OVERRIDE + STATIC_LIGHT_ID_COUNT_OVERRIDE; j++)
        {
            if (j < DYNAMIC_LIGHT_ID_COUNT_OVERRIDE)
            {
                CL_ShadowAtlas.s_dynamicIDPool.Add(j);
                continue;
            }

            CL_ShadowAtlas.s_staticIDPool.Add(j);
        }
    }
}

[HarmonyPatch(typeof(CL_ShadowAtlas), nameof(CL_ShadowAtlas.SetupTextures))]
public class LightExpansionTestAtlasThing
{
    public static readonly string PatchGroup = PatchManager.PatchGroups.REQUIRED;
    
    public static void Postfix(CL_ShadowAtlas __instance)
    {
        Plugin.L.LogWarning($"{nameof(LightExpansionTestAtlasThing)} Postfix running! Overriding atlas resolution!");
        
        var atlas = CL_ShadowAtlas.AtlasTexture;
        // default is: *8, *5 => 40
        CL_ShadowAtlas.SetupTexture(ref atlas, "ShadowAtlas", CL_ShadowAtlas.cShadowResolution * 8, CL_ShadowAtlas.cShadowResolution * 8, 0, RenderTextureFormat.RGHalf);
        CL_ShadowAtlas.AtlasTexture = atlas;
    }
}
*/