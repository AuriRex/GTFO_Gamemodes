using HarmonyLib;
using LevelGeneration;

namespace HNS.Patches;

[HarmonyPatch(typeof(LG_SecurityDoor_Locks), nameof(LG_SecurityDoor_Locks.OnDoorState))]
public class LG_SecurityDoor_Locks__OnDoorState__Patch
{
    // public void OnDoorState(pDoorState state, bool isDropinState = false)
    public static void Postfix(LG_SecurityDoor_Locks __instance)
    {
        __instance.m_intCustomMessage.SetActive(false);
        __instance.m_intHack.SetActive(false);
        __instance.m_intOpenDoor.SetActive(false);
        __instance.m_intUseKeyItem.SetActive(false);
    }
}