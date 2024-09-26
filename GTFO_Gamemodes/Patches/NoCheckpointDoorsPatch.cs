using HarmonyLib;
using LevelGeneration;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.Setup))]
internal class NoCheckpointDoorsPatch
{
    public static readonly string PatchGroup = PatchGroups.NO_CHECKPOINTS;

    public static void Prefix(LG_Gate gate)
    {
        gate.IsCheckpointDoor = false;
    }
}
