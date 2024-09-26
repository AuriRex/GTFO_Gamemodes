namespace Gamemodes.Mode.Tests;

internal class ModeNoSleepers : GamemodeBase
{
    public override string ID => "NoSleepers";

    public override string DisplayName => "No Sleeping Enemies";

    public override ModeSettings Settings => new ModeSettings
    {
        PreventDefaultFailState = true,
        PreventExpeditionEnemiesSpawning = true,
        PreventRespawnRoomsRespawning = true,
        AllowForcedTeleportation = true
    };
}
