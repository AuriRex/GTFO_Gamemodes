using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(PUI_CommunicationMenu), nameof(PUI_CommunicationMenu.IsCmdRelevantCarry))]
public static class PUI_CommunicationMenu__IsCmdRelevantCarry__Patch
{
    public static bool Prefix(ref bool __result)
    {
        // Prevents abuse of 'Q2', seeing if someone is in the same room as a big pickup.
        __result = false;
        return false;
    }
}