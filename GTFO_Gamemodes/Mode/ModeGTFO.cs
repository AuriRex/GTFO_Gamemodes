﻿namespace Gamemodes.Mode;

public class ModeGTFO : GamemodeBase
{
    public override string ID => "BaseGameGTFO";

    public override string DisplayName => "Normal GTFO";

    public override string Description => "Just plain simple old normal GTFO.\nNo alterations.";

    public override string SubTitle => "Vanilla";

    public override ModeSettings Settings => new()
    {
        PreventDefaultFailState = false,
        AllowForcedTeleportation = false,
    };



    public override void Enable()
    {
        PatchManager.DefaultLock = false;
        PatchManager.ApplyPatchGroup(PatchManager.DEFAULT_PATCHGROUP, false);
        PatchManager.DefaultLock = true;
    }

    public override void Disable()
    {
        PatchManager.ApplyPatchGroup(PatchManager.DEFAULT_PATCHGROUP, true);
    }
}
