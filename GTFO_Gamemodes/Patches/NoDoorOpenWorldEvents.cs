using HarmonyLib;
using LevelGeneration;
using static Gamemodes.PatchManager;

namespace Gamemodes.Patches;

[HarmonyPatch(typeof(LG_SecurityDoor), nameof(LG_SecurityDoor.OnDoorIsOpened))]
internal class NoDoorOpenWorldEvents
{
    public static readonly string PatchGroup = PatchGroups.NO_WORLDEVENTS;

    public static bool Prefix()
    {
        return false;
    }
}
