using Agents;
using BepInEx.Unity.IL2CPP.Utils.Collections;
using Gamemodes.Net;
using HNS.Core;
using HNS.Net;
using Il2CppInterop.Runtime.Attributes;
using Player;
using SNetwork;
using System;
using System.Collections;
using UnityEngine;

namespace HNS.Components;

public class HideAndSeekGameManager : MonoBehaviour
{
    private float _countdown = 1;
    private int _countdownInt = 0;
    private int _countdownMax = 60;
    private Func<StyleOverride> _countdownStyleProvider;

    private float _gameTimer = 0;
    private int _gameTimerInt = 0;

    private const string COUNTDOWN_TIMER_MARKER = "[COUNTDOWN]";
    private const string DEFAULT_COUNTDOWN_FORMATTEXT = $"{COUNTDOWN_TIMER_MARKER} until seekers are released.";

    private string _countdownTextFormat = DEFAULT_COUNTDOWN_FORMATTEXT;
    private string _messageText = string.Empty;

    private Blinds _blinds;

    private bool _hasImportantMessage = false;
    private string _importantMessage = "??:??";
    private Coroutine _importantMessageDisplayCoroutine;

    private bool _localPlayerIsSeeker;
    private bool _startedAsSeeker;

    private Session _session;
    private bool _messageVisible;

    [HideFromIl2Cpp]
    public void StartGame(bool localPlayerIsSeeker, byte blindDuration, Session session)
    {
        _session = session;
        StartCountdown(blindDuration, GetDefaultGameStartCountdownStyle);
        _gameTimer = 0;
        _gameTimerInt = -1;
        _hasImportantMessage = false;
        _localPlayerIsSeeker = localPlayerIsSeeker;
        _startedAsSeeker = localPlayerIsSeeker;

        _countdownStyleProvider = GetDefaultGameStartCountdownStyle;

        InstantReviveLocalPlayer();

        Blinds blinds = null;
        if (localPlayerIsSeeker)
        {
            blinds = BlindPlayer();
        }

        CoroutineManager.StartCoroutine(UnblindPlayer(blindDuration, blinds).WrapToIl2Cpp());
    }

    private void InstantReviveLocalPlayer()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return;

        var ploc = localPlayer.Locomotion;

        if (ploc.m_currentStateEnum != PlayerLocomotion.PLOC_State.Downed)
        {
            ploc.ChangeState(PlayerLocomotion.PLOC_State.Stand, wasWarpedIntoState: false);
        }

        localPlayer.Damage.AddHealth(localPlayer.Damage.HealthMax, localPlayer);
    }

    [HideFromIl2Cpp]
    public void OnLocalPlayerCaught()
    {
        if (!_session.IsActive)
            return;

        if (!_localPlayerIsSeeker)
        {
            _localPlayerIsSeeker = true;
            _session.LocalPlayerCaught();
            StartCountdown(10, StyleRed, $"You've been caught!\n<color=orange>Time spent hiding: {_session.HidingTime.ToString(@"mm\:ss")}</color>");
        }

        CoroutineManager.StartCoroutine(ReviveSeekerRoutine().WrapToIl2Cpp());
    }

    [HideFromIl2Cpp]
    private IEnumerator ReviveSeekerRoutine()
    {
        var yielder = new WaitForSeconds(5);
        yield return yielder;

        if (!NetworkingManager.InLevel)
            yield break;

        InstantReviveLocalPlayer();
    }

    [HideFromIl2Cpp]
    internal void StopGame(Session session)
    {
        if (_session != session)
        {
            Plugin.L.LogWarning("Stopped session is not the same as the started one?? This should not happen!");
        }

        var message = $"Game Over! Total time: {session.FinalTime.ToString(@"mm\:ss")}";

        if (!_startedAsSeeker)
        {
            message = $"{message}\n<color=orange>You hid for: {session.HidingTime.ToString(@"mm\:ss")}</color>";
        }

        //StartNewImportantMessageCoroutine(message);
        StartCountdown(10, StyleImportant, message);

        _blinds?.Dispose();
        _blinds = null;

        if (SNet.IsMaster)
        {
            CoroutineManager.StartCoroutine(SwitchToHiderRoutine().WrapToIl2Cpp());
        }
    }

    private IEnumerator SwitchToHiderRoutine()
    {
        if (!SNet.IsMaster)
            yield break;

        var yielder = new WaitForSeconds(5);
        yield return yielder;

        foreach(var player in SNet.LobbyPlayers)
        {
            NetworkingManager.AssignTeam(player, (int)GMTeam.Hiders);
        }
    }

    [HideFromIl2Cpp]
    private StyleOverride StyleDefault()
    {
        return new StyleOverride(false, CStyle.Default, false);
    }

    [HideFromIl2Cpp]
    private StyleOverride StyleImportant()
    {
        return new StyleOverride(true, CStyle.Green, true);
    }

    [HideFromIl2Cpp]
    private StyleOverride StyleRed()
    {
        return new StyleOverride(false, CStyle.Red, true);
    }

    [HideFromIl2Cpp]
    private void StartNewImportantMessageCoroutine(string message, int displayTime = 10)
    {
        _importantMessage = message;

        if (_importantMessageDisplayCoroutine != null)
        {
            CoroutineManager.StopCoroutine(_importantMessageDisplayCoroutine);
        }

        _importantMessageDisplayCoroutine = CoroutineManager.StartCoroutine(ImportantMessageCoroutine(displayTime).WrapToIl2Cpp());
    }

    [HideFromIl2Cpp]
    private IEnumerator ImportantMessageCoroutine(int displayTime = 10)
    {
        _hasImportantMessage = true;

        var yielder = new WaitForSeconds(displayTime);
        yield return yielder;

        _hasImportantMessage = false;
        GuiManager.InteractionLayer.MessageVisible = false;
    }

    [HideFromIl2Cpp]
    private Blinds BlindPlayer()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return null;

        _blinds?.Dispose();
        _blinds = new(localPlayer.TryCast<LocalPlayerAgent>());
        return _blinds;
    }

    [HideFromIl2Cpp]
    private IEnumerator UnblindPlayer(byte blindDuration, Blinds blinds)
    {
        var yielder = new WaitForSeconds(blindDuration);
        yield return yielder;

        blinds?.Dispose();
        if (_blinds == blinds)
            _blinds = null;

        StartCountdown(10, StyleImportant, "Seekers have been released!");
    }

    [HideFromIl2Cpp]
    public void StartCountdown(int duration, Func<StyleOverride> styleProvider, string formatText = null)
    {
        _countdown = duration;
        _countdownMax = duration;
        _countdownInt = -1;
        _countdownStyleProvider = styleProvider ?? GetDefaultGameStartCountdownStyle;

        if (string.IsNullOrWhiteSpace(formatText))
        {
            formatText = DEFAULT_COUNTDOWN_FORMATTEXT;
        }

        _countdownTextFormat = formatText;
    }

    public void Awake()
    {

    }

    public void Update()
    {
        var sessionActive = _session != null && _session.IsActive;

        if (sessionActive)
        {
            if (_session.SetupTimeFinished)
            {
                _gameTimer += Time.deltaTime;
            }
        }

        CStyle style = CStyle.Default;
        bool doBlink = false;
        bool showCountdownTimer = _countdown > 0;

        if (showCountdownTimer)
        {
            _countdown -= Time.deltaTime;
        }

        if (_hasImportantMessage)
        {
            _messageText = _importantMessage;
            showCountdownTimer = false;
            doBlink = true;
            style = CStyle.Green;
        }
        else
        {
            if (showCountdownTimer)
            {
                DisplayCountdownTimer(ref style, ref doBlink);
            }
            else
            {
                DisplayGameTimer(ref style, ref doBlink);
            }
        }

        if (!sessionActive && !showCountdownTimer && !_hasImportantMessage)
        {
            if (_messageVisible)
            {
                GuiManager.InteractionLayer.MessageVisible = false;
                _messageVisible = false;
            }
            return;
        }

        GuiManager.InteractionLayer.SetMessage(_messageText, ePUIMessageStyle.Message, priority: 10);
        GuiManager.InteractionLayer.SetMessageTimer(_countdown / _countdownMax);
        GuiManager.InteractionLayer.SetTimerAlphaMul(showCountdownTimer ? 1f : 0f);
        GuiManager.InteractionLayer.MessageTimerVisible = showCountdownTimer;
        GuiManager.InteractionLayer.MessageVisible = true;
        _messageVisible = true;

        switch (style)
        {
            default:
            case CStyle.Default:
                SetStyle("CCC", Color.gray, blinking: doBlink);
                break;
            case CStyle.Green:
                SetStyle("0C0", Color.green, blinking: doBlink);
                break;
            case CStyle.Warning:
                SetStyle("FC0", Color.yellow, blinking: doBlink);
                break;
            case CStyle.Red:
                SetStyle("F00", Color.red, blinking: doBlink);
                break;
        }
    }

    [HideFromIl2Cpp]
    private void DisplayGameTimer(ref CStyle style, ref bool doBlink)
    {
        var rounded = Mathf.RoundToInt(_gameTimer);

        if (rounded == _gameTimerInt)
            return;

        _gameTimerInt = rounded;

        var prefix = "Hiding for: ";

        if (_localPlayerIsSeeker)
        {
            prefix = "Seeking for: ";
        }

        _messageText = $"{prefix}{TimeSpan.FromSeconds(_gameTimerInt).ToString(@"mm\:ss")}";

        if (_gameTimerInt % 300 <= 5)
        {
            style = CStyle.Warning;
            doBlink = true;
        }
    }

    [HideFromIl2Cpp]
    private void DisplayCountdownTimer(ref CStyle style, ref bool doBlink)
    {
        var rounded = Mathf.RoundToInt(_countdown);

        if (rounded != _countdownInt)
        {
            _countdownInt = rounded;
            _messageText = _countdownTextFormat.Replace(COUNTDOWN_TIMER_MARKER, TimeSpan.FromSeconds(_countdownInt).ToString(@"mm\:ss"));
        }

        var result = _countdownStyleProvider?.Invoke();

        if (result.HasValue)
        {
            var styleOverride = result.Value;

            if (styleOverride.doOverride)
            {
                style = styleOverride.style;
                doBlink = styleOverride.doBlink;
            }
        }
    }

    [HideFromIl2Cpp]
    private StyleOverride GetDefaultGameStartCountdownStyle()
    {
        var ret = new StyleOverride(false, CStyle.Default, false);

        if (_countdown <= 10 && _countdown > 0)
        {
            ret.style = CStyle.Red;
            ret.doBlink = true;
            ret.doOverride = true;
        }
        else if (_countdown <= 20 && _countdown > 0)
        {
            ret.style = CStyle.Warning;
            ret.doOverride = true;
        }

        return ret;
    }

    public record struct StyleOverride(bool doOverride, CStyle style, bool doBlink);

    public enum CStyle
    {
        Default,
        Green,
        Warning,
        Red,
    }

    [HideFromIl2Cpp]
    private static void SetStyle(string textHex, Color color, bool blinking = false)
    {
        var msg = GuiManager.InteractionLayer.m_message;

        msg.m_colorHex = textHex;
        msg.m_timer.color = new Color(color.r, color.g, color.b, msg.m_timerAlpha);
        msg.m_blinking = blinking;
    }
}
