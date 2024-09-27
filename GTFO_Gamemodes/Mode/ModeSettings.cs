namespace Gamemodes.Mode;

public class ModeSettings
{
    public bool PreventDefaultFailState;
    public bool AllowForcedTeleportation;
    public bool AllowMidGameModeSwitch;
    public bool PreventExpeditionEnemiesSpawning;
    public bool PreventRespawnRoomsRespawning;
    public bool PreventPlayerRevives; // TODO
    public bool OpenAllSecurityDoors;
    public bool OpenAllWeakDoors;
    public bool RemoveCheckpoints;
    public bool BlockWorldEvents;
    public bool RevealEntireMap;
    public Utils.MapIconTypes MapIconsToReveal = Utils.MapIconTypes.None;
    public bool ForceAddArenaDimension;
    public bool DisableVoiceLines; // TODO
}
