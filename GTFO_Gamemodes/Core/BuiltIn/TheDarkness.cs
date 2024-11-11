using System.Collections;
using System.Linq;
using System.Runtime.InteropServices;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Components;
using Gamemodes.Extensions;
using Gamemodes.Net;
using Il2CppInterop.Runtime.Injection;
using Player;
using SNetwork;
using UnityEngine;

namespace Gamemodes.Core.BuiltIn;

public partial class TheDarkness : GamemodeBase
{
    public override string ID => "the_darkness";

    public override string DisplayName => "The Darkness";

    public override string Description => "";

    public override string SubTitle => "I see no evil.";

    public override ModeSettings Settings => new ModeSettings
    {
        
    };

    private TimerHUD _timer;

    private static bool _isTimeToBecomeTheChosenOne;
    
    public override void Init()
    {
        ClassInjector.RegisterTypeInIl2Cpp<Blinder>();

        _timer = gameObject.AddComponent<TimerHUD>();
        
        NetworkingManager.RegisterEvent<pChosenOne>(ChosenOneReceived);
    }

    public override void Enable()
    {
        Patch();
        GameEvents.OnGameStateChanged += OnGameStateChanged;
    }

    public override void Disable()
    {
        GameEvents.OnGameStateChanged -= OnGameStateChanged;
        Unpatch();
    }

    private Coroutine _gameStartCoroutine;

    public static int SETUP_TIME = 6;
    
    private void OnGameStateChanged(eGameStateName state)
    {
        switch (state)
        {
            case eGameStateName.InLevel:
                _isTimeToBecomeTheChosenOne = true;
                _timer.StartCountdown(SETUP_TIME, $"{nameof(TheDarkness)} starting in {TimerHUD.COUNTDOWN_TIMER_MARKER}\n<color=orange>Jump</color> to become <i>the chosen one</i>!", () => TimerHUD.TSO_RED_WARNING);
                _gameStartCoroutine = CoroutineManager.StartCoroutine(SetupCoroutine().WrapToIl2Cpp());
                break;
            case eGameStateName.Lobby:
            {
                _isTimeToBecomeTheChosenOne = false;
                _hasChosenOneBeenFound = false;
            
                StopCoroutine();

                PlayerManager.GetLocalPlayerAgent()?.gameObject.GetComponent<Blinder>()?.SafeDestroy();
                
                break;
            }
        }
    }

    private void StopCoroutine()
    {
        if (_gameStartCoroutine == null)
        {
            return;
        }

        CoroutineManager.StopCoroutine(_gameStartCoroutine);
        _gameStartCoroutine = null;
    }

    private bool _hasChosenOneBeenFound;
    
    private void ChosenOneReceived(ulong sender, pChosenOne data)
    {
        if (!NetworkingManager.InLevel)
            return;
        
        NetworkingManager.GetPlayerInfo(sender, out var senderInfo);

        if (!SNet.IsMaster || _hasChosenOneBeenFound)
        {
            if (!senderInfo.IsMaster)
                return;

            NetworkingManager.GetPlayerInfo(data.ChosenPlayerID, out var chosenOne);
            
            StopCoroutine();
            
            _timer.StartCountdown(5, $"Chosen one has been found:\n<color=orange>{chosenOne.PlayerColorTag}{chosenOne.NickName}</color> is the chosen one.</color>\nGood Luck!", () => TimerHUD.TSO_GREEN_BLINKING);
            
            if (chosenOne.IsLocal)
                return;

            PlayerManager.GetLocalPlayerAgent().gameObject.GetOrAddComponent<Blinder>();

            return;
        }

        if (_hasChosenOneBeenFound)
            return;

        SendChosenOne(data.ChosenPlayerID);
    }

    private void SendChosenOne(ulong? chosenOneId = null)
    {
        if (_hasChosenOneBeenFound)
            return;
        
        _hasChosenOneBeenFound = true;

        if (!chosenOneId.HasValue)
        {
            var players = PlayerManager.PlayerAgentsInLevel.ToArray();
            
            chosenOneId = players[Random.Range(0, players.Length - 1)].Owner.Lookup;
            Plugin.L.LogDebug($"Picked random chosen player: {chosenOneId}");
        }
        
        NetworkingManager.SendEvent(new pChosenOne
        {
            ChosenPlayerID = chosenOneId.Value,
        }, invokeLocal: true);
    }
    
    private IEnumerator SetupCoroutine()
    {
        var yielder = new WaitForSeconds(SETUP_TIME);
        yield return yielder;
        _isTimeToBecomeTheChosenOne = false;

        if (!SNet.IsMaster)
            yield break;
        
        SendChosenOne();
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct pChosenOne
    {
        [MarshalAs(UnmanagedType.U8)]
        public ulong ChosenPlayerID;
    }
}