using Gamemodes.Components;
using Gamemodes.Extensions;
using HarmonyLib;
using Player;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches.Required;

[HarmonyPatch(typeof(LocalPlayerAgent), nameof(LocalPlayerAgent.Setup))]
public class LocalPlayerAgent__Setup__Patch
{
    public static readonly string PatchGroup = PatchGroups.REQUIRED;
    
    public static void Postfix(LocalPlayerAgent __instance)
    {
        var nodeDistance = __instance.gameObject.GetOrAddComponent<NodeDistance>();

        nodeDistance.enabled = false;
    }
}