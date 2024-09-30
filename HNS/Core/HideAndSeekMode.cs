using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes;
using Gamemodes.Mode;
using Gamemodes.Net;
using HarmonyLib;
using HNS.Components;
using HNS.Net;
using Il2CppInterop.Runtime.Injection;
using System;
using System.Collections;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace HNS.Core;

internal class HideAndSeekMode : GamemodeBase
{
    public static HideAndSeekGameManager GameManager { get; private set; }

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
        ForceAddArenaDimension = true,
        DisableVoiceLines = true,
        UseTeamVisibility = true,
    };

    private Harmony _harmonyInstance;

    private GameObject _gameManagerGO;

    public override void Init()
    {
        _harmonyInstance = new Harmony(Plugin.GUID);
        NetSessionManager.Init();

        Gamemodes.Patches.PlayerChatManager_PostMessage_Patch.AddCommand("hnsstart", ((Func<string[], string>)StartHNS).Method);
        Gamemodes.Patches.PlayerChatManager_PostMessage_Patch.AddCommand("hnsstop", ((Func<string[], string>)StopHNS).Method);

        TeamVisibility.Team((int)GMTeam.Seekers).CanSeeSelf();

        _gameManagerGO = new GameObject("HideAndSeek_Manager");

        UnityEngine.Object.DontDestroyOnLoad(_gameManagerGO);
        _gameManagerGO.hideFlags = HideFlags.HideAndDontSave | HideFlags.DontUnloadUnusedAsset;

        _gameManagerGO.SetActive(false);

        if (!ClassInjector.IsTypeRegisteredInIl2Cpp<HideAndSeekGameManager>())
            ClassInjector.RegisterTypeInIl2Cpp<HideAndSeekGameManager>();

        GameManager = _gameManagerGO.AddComponent<HideAndSeekGameManager>();
    }

    public static string StartHNS(string[] args)
    {
        var seekers = NetworkingManager.AllValidPlayers.Where(pw => pw.Team == (int)GMTeam.Seekers).Select(pw => pw.ID).ToArray();

        NetSessionManager.SendStartGamePacket(seekers);

        return $"Start Game Packet sent! -> {seekers.Length} Seekers";
    }

    public static string StopHNS(string[] args)
    {
        NetSessionManager.SendStopGamePacket();
        return "";
    }

    public override void Enable()
    {
        _harmonyInstance.PatchAll(Assembly.GetExecutingAssembly());
        _gameManagerGO.SetActive(true);

        GameEvents.OnGameSessionStart += GameEvents_OnGameSessionStart;
        GameEvents.OnGameStateChanged += GameEvents_OnGameStateChanged;
    }

    public override void Disable()
    {
        _gameManagerGO.SetActive(false);
        _harmonyInstance.UnpatchSelf();

        GameEvents.OnGameSessionStart -= GameEvents_OnGameSessionStart;
        GameEvents.OnGameStateChanged -= GameEvents_OnGameStateChanged;
    }

    private void GameEvents_OnGameStateChanged(eGameStateName state)
    {
        /*if (state == eGameStateName.InLevel)
        {
            // TODO: Remove later
            CoroutineManager.StartCoroutine(DoThing().WrapToIl2Cpp());
        }*/
    }

    private static IEnumerator DoThing()
    {
        Plugin.L.LogWarning($"Starting game test thingie in 5 seconds!");
        yield return new WaitForSeconds(5);
        Plugin.L.LogWarning($"Starting game test thingie :3c");
        NetSessionManager.SendStartGamePacket(NetworkingManager.LocalPlayerId);
    }

    private void GameEvents_OnGameSessionStart()
    {
        // TODO: Not this xd
        Plugin.L.LogWarning("Hi o/");
    }
}