namespace Gamemodes.Mode.Tests;

#if DEBUG
internal class ModeNoSleepers : GamemodeBase
{
    public override string ID => "NoSleepers";

    public override string DisplayName => "No Sleeping Enemies";

    public override string Description => "Test Gamemode\nDoes not spawn any sleeping enemies";

    public override string SubTitle => "Hi o/";

    public override ModeSettings Settings => new ModeSettings
    {
        PreventDefaultFailState = true,
        PreventExpeditionEnemiesSpawning = true,
        PreventRespawnRoomsRespawning = true,
        AllowForcedTeleportation = true
    };
}
#endif