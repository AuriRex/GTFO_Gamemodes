namespace Gamemodes.Mode.Tests;

#if DEBUG
internal class ModeTesting : GamemodeBase
{
    public override string ID => "GMTesting";

    public override string DisplayName => "Test Mode";

    public override string Description => "This here is a description with a pretty long first line I guess,\nand\nafter\nthat\nsingle\nlong\nline\nit\ngoes\ndown\nquite\na\nbit\n:D";

    public override string SubTitle => "Ayo? Subtitle!";

    public override ModeSettings Settings => new ModeSettings
    {
        AllowMidGameModeSwitch = true,
        PreventDefaultFailState = true,
        AllowForcedTeleportation = true,
    };
}
#endif
