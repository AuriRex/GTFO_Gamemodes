using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(MineDeployerInstance), nameof(MineDeployerInstance.Setup))]
public static class MineDeployerPatches
{
    public static void Postfix(MineDeployerInstance __instance)
    {
        
    }
}