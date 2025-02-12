using UnityEngine;

namespace Gamemodes.Core.TestModes;

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
    
    public override Color? GetElevatorColor() => new Color(0.2f, 0.2f, 1f, 1f) * 0.6f;
}
#endif