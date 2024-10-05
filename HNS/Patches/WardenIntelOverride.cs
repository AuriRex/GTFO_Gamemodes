using HarmonyLib;

namespace HNS.Patches;

[HarmonyPatch(typeof(PlayerGuiLayer), nameof(PlayerGuiLayer.ShowWardenIntel))]
public class WardenIntelOverride
{
    private static bool _doRun = false;

    public static bool Prefix()
    {
        return _doRun;
    }

    public static void ForceShowWardenIntel(string intel, float delay = 0f, float duration = 6f)
    {
        _doRun = true;
        GuiManager.PlayerLayer.ShowWardenIntel(intel, delay, duration);
        _doRun = false;
    }
}
