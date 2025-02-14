using CellMenu;
using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(CM_PageMap), nameof(CM_PageMap.OnEnable))]
public class MapNoLegendPatch
{
    public static void Postfix(CM_PageMap __instance)
    {
        __instance.m_mapLegend.gameObject.SetActive(false);
    }
}