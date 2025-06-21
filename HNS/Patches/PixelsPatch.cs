using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using HarmonyLib;
using UnityEngine;

namespace HNS.Patches;

[HarmonyPatch(typeof(CM_MapDrawPixel), nameof(CM_MapDrawPixel.StartFadeOut))]
public class PixelsPatch
{
    public static float DisplayDuration { get; set; } = 110f;
    public static float FadeoutDuration { get; set; } = 10f;
    
    public const float DEFAULT_DISPLAY_DURATION = 0f;
    public const float DEFAULT_FADEOUT_DURATION = 3f;
    
    public static bool Prefix(CM_MapDrawPixel __instance)
    {
        __instance.StopAllCoroutines();
        __instance.m_pixel.color = __instance.m_colorStart;
        __instance.gameObject.SetActive(true);
        __instance.StartCoroutine(FadeOutRoutine(__instance, DisplayDuration, FadeoutDuration).WrapToIl2Cpp());
        return false;
    }

    private static IEnumerator FadeOutRoutine(CM_MapDrawPixel mapDrawPixel, float duration = 0f, float fadeoutDuration = 3f)
    {
        if (duration > 0f)
        {
            yield return new WaitForSeconds(duration);
        }

        if (fadeoutDuration <= 0f)
        {
            fadeoutDuration = DEFAULT_FADEOUT_DURATION;
        }
        
        var time = 0f;
        mapDrawPixel.m_pixel.color = mapDrawPixel.m_colorStart;
        while (time <= fadeoutDuration)
        {
            var t = Easing.EaseInExpo(time, 0f, 1f, fadeoutDuration);
            mapDrawPixel.m_pixel.color = Color.Lerp(mapDrawPixel.m_colorStart, mapDrawPixel.m_colorFadeOut, t);
            time += Clock.Delta;
            yield return null;
        }
        mapDrawPixel.m_pixel.color = mapDrawPixel.m_colorFadeOut;
        mapDrawPixel.StopAllCoroutines();
        mapDrawPixel.gameObject.SetActive(false);
    }
}