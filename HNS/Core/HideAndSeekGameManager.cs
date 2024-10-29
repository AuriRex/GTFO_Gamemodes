using System.Collections;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Components;
using Gamemodes.Net;
using Gamemodes.UI;
using HNS.Net;
using Player;
using SNetwork;
using UnityEngine;

namespace HNS.Core;

public class HideAndSeekGameManager
{
    public HideAndSeekGameManager(TimerHUD timer)
    {
        SetTimerHud(timer);
    }

    private Blinds _blinds;

    private Coroutine _unblindPlayerCoroutine;

    private bool _localPlayerIsSeeker;
    private bool _startedAsSeeker;

    private Session _session;

    private TimerHUD _gameTimerDisplay;

    private const string GAMESTART_COUNTDOWN_FORMATTEXT = $"{TimerHUD.COUNTDOWN_TIMER_MARKER} until seekers are released.";
    
    public void SetTimerHud(TimerHUD timer)
    {
        _gameTimerDisplay = timer;
    }
    
    public void StartGame(bool localPlayerIsSeeker, byte blindDuration, Session session)
    {
        if (_session != null && _session.IsActive)
        {
            _session.EndSession();
            StopGame(_session);
        }

        _session = session;
        _gameTimerDisplay.ResetGameTimer();
        _gameTimerDisplay.StartGameTimer();
        _gameTimerDisplay.StartCountdown(blindDuration, GetDefaultGameStartCountdownStyle, GAMESTART_COUNTDOWN_FORMATTEXT);

        _localPlayerIsSeeker = localPlayerIsSeeker;
        _startedAsSeeker = localPlayerIsSeeker;

        HideAndSeekMode.InstantReviveLocalPlayer();

        Blinds blinds = null;
        if (localPlayerIsSeeker)
        {
            blinds = BlindPlayer();
        }

        _unblindPlayerCoroutine = CoroutineManager.StartCoroutine(GameStartSetupTimeCoroutine(blindDuration, blinds).WrapToIl2Cpp());
    }
    
    public bool OnLocalPlayerCaught()
    {
        if (_session == null || !_session.IsActive)
            return false;

        if (!_localPlayerIsSeeker)
        {
            _localPlayerIsSeeker = true;
            _session.LocalPlayerCaught();
            var hiddenForMsg = $"<color=orange>Time spent hiding: {_session.HidingTime.ToString(@"mm\:ss")}</color>";
            _gameTimerDisplay.StartCountdown(5, StyleRed, $"You've been caught!\n{hiddenForMsg}\nReviving in {TimerHUD.COUNTDOWN_TIMER_MARKER}");
            Gamemodes.Plugin.PostLocalMessage(hiddenForMsg);
        }
        else
        {
            _gameTimerDisplay.StartCountdown(5, StyleRed, $"Reviving in {TimerHUD.COUNTDOWN_TIMER_MARKER}");
        }

        CoroutineManager.StartCoroutine(RevivePlayerRoutine(5).WrapToIl2Cpp());
        return true;
    }

    public void ReviveLocalPlayer(int reviveDelay = 5, bool showSeekerMessage = false)
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return;

        if (localPlayer.Locomotion.m_currentStateEnum != PlayerLocomotion.PLOC_State.Downed)
            return;

        _gameTimerDisplay.StartCountdown(reviveDelay, StyleDefault, $"Reviving in {TimerHUD.COUNTDOWN_TIMER_MARKER}");

        CoroutineManager.StartCoroutine(RevivePlayerRoutine(5, showSeekerMessage).WrapToIl2Cpp());
    }

    private IEnumerator RevivePlayerRoutine(int reviveDelay, bool showSeekerMessage = true)
    {
        if (reviveDelay > 0)
        {
            var yielder = new WaitForSeconds(reviveDelay);
            yield return yielder;
        }

        if (!NetworkingManager.InLevel)
            yield break;

        HideAndSeekMode.InstantReviveLocalPlayer();

        if (_session != null && _session.IsActive && showSeekerMessage)
        {
            _gameTimerDisplay.StartCountdown(5, StyleRed, $"You've been caught!\nFind the remaining hiders!");
        }
    }

    internal void StopGame(Session session, bool aborted = false)
    {
        if (_session != session)
        {
            Plugin.L.LogWarning("Stopped session is not the same as the started one?? This should not happen!");
        }

        _gameTimerDisplay.StopGameTimer();
        
        if (_unblindPlayerCoroutine != null)
        {
            CoroutineManager.StopCoroutine(_unblindPlayerCoroutine);
            _unblindPlayerCoroutine = null;
        }

        _blinds?.Dispose();
        _blinds = null;

        var message = $"Game Over! Total time: {session.FinalTime.ToString(@"mm\:ss")}";
        Gamemodes.Plugin.PostLocalMessage("<#0f0>-= Game Over! =-</color>");
        Gamemodes.Plugin.PostLocalMessage($"<color=white>Total Game Time: {session.FinalTime.ToString(@"mm\:ss")}</color>");

        if (!_startedAsSeeker)
        {
            var hid = $"<color=orange>You hid for: {session.HidingTime.ToString(@"mm\:ss")}</color>";
            message = $"{message}\n{hid}";
            Gamemodes.Plugin.PostLocalMessage(hid);
        }

        if (aborted)
        {
            Gamemodes.Plugin.PostLocalMessage("<color=red>Game aborted!</color>");
            return;
        }

        _gameTimerDisplay.StartCountdown(10, StyleImportant, message);

        if (SNet.IsMaster)
        {
            CoroutineManager.StartCoroutine(PostGameTeamSwitchCoroutine().WrapToIl2Cpp());
        }
    }

    private IEnumerator PostGameTeamSwitchCoroutine()
    {
        if (!SNet.IsMaster)
            yield break;

        var session = _session;

        var yielder = new WaitForSeconds(5);
        yield return yielder;

        if (session != _session)
            yield break;

        foreach(var player in SNet.LobbyPlayers)
        {
            NetworkingManager.AssignTeam(player, (int)GMTeam.PreGameAndOrSpectator);
        }
    }

    private TimerStyleOverride StyleDefault()
    {
        return new TimerStyleOverride(false, TimerDisplayStyle.Default, false);
    }
    
    private TimerStyleOverride StyleImportant()
    {
        return new TimerStyleOverride(true, TimerDisplayStyle.Green, true);
    }

    private TimerStyleOverride StyleRed()
    {
        return new TimerStyleOverride(false, TimerDisplayStyle.Red, true);
    }

    private Blinds BlindPlayer()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return null;

        _blinds?.Dispose();
        _blinds = new(localPlayer.TryCast<LocalPlayerAgent>());
        return _blinds;
    }

    private IEnumerator GameStartSetupTimeCoroutine(byte setupDuration, Blinds blinds)
    {
        var yielder = new WaitForSeconds(setupDuration);
        yield return yielder;

        blinds?.Dispose();
        if (_blinds == blinds)
            _blinds = null;

        _gameTimerDisplay.StartCountdown(10, StyleImportant, "Seekers have been released!");
    }

    private TimerStyleOverride GetDefaultGameStartCountdownStyle()
    {
        var ret = new TimerStyleOverride(false, TimerDisplayStyle.Default, false);
        
        switch (_gameTimerDisplay.Countdown)
        {
            case <= 10 and > 0:
                ret.Style = TimerDisplayStyle.Red;
                ret.DoBlink = true;
                ret.DoOverride = true;
                break;
            case <= 20 and > 10:
                ret.Style = TimerDisplayStyle.Warning;
                ret.DoOverride = true;
                break;
        }

        return ret;
    }
}
