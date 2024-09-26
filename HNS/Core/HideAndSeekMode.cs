using Gamemodes;
using Gamemodes.Mode;
using HarmonyLib;
using HNS.Net;
using System.Reflection;

namespace HNS.Core;

internal class HideAndSeekMode : GamemodeBase
{
    public override string ID => "hideandseek";

    public override string DisplayName => "Hide 'n' Seek";

    public override ModeSettings Settings => new ModeSettings
    {
        AllowMidGameModeSwitch = false,
        PreventDefaultFailState = true,
        PreventExpeditionEnemiesSpawning = true,
        PreventPlayerRevives = true,
        PreventRespawnRoomsRespawning = true,
        BlockWorldEvents = true,
        OpenAllSecurityDoors = true,
        OpenAllWeakDoors = true,
        RemoveCheckpoints = true,
        AllowForcedTeleportation = true,
        RevealEntireMap = true,
        MapIconsToReveal = Utils.EVERYTHING_EXCEPT_LOCKERS,
    };

    private Harmony _harmonyInstance;

    public override void Init()
    {
        _harmonyInstance = new Harmony(Plugin.GUID);
        NetSessionManager.Init();
    }

    public override void Enable()
    {
        _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        GameEvents.OnGameSessionStart += GameEvents_OnGameSessionStart;
    }

    public override void Disable()
    {
        _harmonyInstance.UnpatchSelf();
        GameEvents.OnGameSessionStart -= GameEvents_OnGameSessionStart;
    }

    private void GameEvents_OnGameSessionStart()
    {
        // TODO: Not this xd
        Plugin.L.LogWarning("Hi o/");
    }
}