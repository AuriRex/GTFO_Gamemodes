using HarmonyLib;
using LevelGeneration;

namespace HNS.Patches;

//InfectionSpitter
[HarmonyPatch(typeof(LG_PlaceStaticEnemyInNode), nameof(LG_PlaceStaticEnemyInNode.Build))]
public class NoStaticThingiesTwoPatch
{
    public static bool Prefix(ref bool __result, LG_PlaceStaticEnemyInNode __instance)
    {
        switch (__instance.m_dataBlock.name)
        {
            case "Respawner_Sack":
            case "InfectionSpitter":
                break;
            default:
                return true;
        }
        
        __result = true;
        return false;
    }
}