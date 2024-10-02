using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(PUI_GameEventLog), nameof(PUI_GameEventLog.OnGameEvent))]
internal static class PUI_GameEventLog_OnGameEvent_Patch
{
    public static bool Prefix()
    {
        return false;
    }
}
