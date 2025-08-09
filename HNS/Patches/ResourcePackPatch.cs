using Gear;
using HarmonyLib;
using UnityEngine;

namespace HNS.Patches;

//public override void Setup(ItemDataBlock data)
[HarmonyPatch(typeof(ResourcePackPickup), nameof(ResourcePackPickup.Setup))]
public class ResourcePackPatch
{
    // Fixes resource packs leaving behind shadows :p
    public static void Postfix(ResourcePackPickup __instance)
    {
        foreach (var renderer in __instance.GetComponentsInChildren<Renderer>())
        {
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
        }
    }
}