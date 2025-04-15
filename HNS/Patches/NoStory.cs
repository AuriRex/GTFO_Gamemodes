using GameData;
using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(WardenObjectiveManager), nameof(WardenObjectiveManager.CheckAndExecuteEventsOnTrigger),
    typeof(WardenObjectiveEventData), typeof(eWardenObjectiveEventTrigger), typeof(bool), typeof(float))]
internal static class WardenObjectiveManager__CheckAndExecuteEventsOnTrigger__Patch
{
    public static void Prefix(WardenObjectiveEventData eventToTrigger)
    {
        if (eventToTrigger.SoundSubtitle.HasValue)
        {
            eventToTrigger.SoundID = 0;
        }
    }
}