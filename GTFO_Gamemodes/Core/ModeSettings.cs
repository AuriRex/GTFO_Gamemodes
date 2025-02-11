namespace Gamemodes.Core;

public class ModeSettings
{
    public bool PreventDefaultFailState;
    public bool AllowForcedTeleportation;
    public bool AllowMidGameModeSwitch;
    public bool PreventExpeditionEnemiesSpawning;
    public bool PreventRespawnRoomsRespawning;
    public bool PreventPlayerRevives;
    public bool OpenAllSecurityDoors;
    public bool OpenAllWeakDoors;
    public bool RemoveCheckpoints;
    public bool BlockWorldEvents;
    public bool RevealEntireMap;
    public Utils.MapIconTypes MapIconsToReveal = Utils.MapIconTypes.None;
    public bool ForceAddArenaDimension;
    public bool DisableVoiceLines;
    public bool UseTeamVisibility;
    public bool RemoveTerminalCommands;
    public bool RemoveBloodDoors;
    public bool InfiniteSentryAmmo;
    public bool InfiniteBackpackAmmo;
    public float InitialPushForceMultiplier = 1f;
    public float InitialSlidePushForceMultiplier = 1f;
    public bool UseProximityVoiceChat = false;
    public bool UseNodeDistance = false;
}
