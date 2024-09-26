namespace Gamemodes.Mode.Tests;

internal class ModeTesting : GamemodeBase
{
    public override string ID => "GMTesting";

    public override string DisplayName => "Test Mode";

    public override ModeSettings Settings => new ModeSettings
    {
        AllowMidGameModeSwitch = true,
        PreventDefaultFailState = true,
        AllowForcedTeleportation = true,
    };
}
