using Gamemodes.Resources;
using UnityEngine;

namespace Gamemodes.Core;

public class ModeGTFO : GamemodeBase
{
    public const string MODE_ID = "GTFO";
    
    public override string ID => MODE_ID;

    public override string DisplayName => "Normal GTFO";

    public override string Description => "Just plain simple old normal GTFO.\nNo alterations.";

    public override string SubTitle => "Vanilla";

    public override Sprite SpriteLarge => _gtfoLogo;
    public override Sprite SpriteSmall => _gtfoLogo;

    public override ModeSettings Settings => new()
    {
        PreventDefaultFailState = false,
        AllowForcedTeleportation = false,
    };
    
    private Sprite _gtfoLogo;

    public override void Init()
    {
        ImageLoader.LoadNewImageSprite(Data.GTFO_Icon, out _gtfoLogo);
    }

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
