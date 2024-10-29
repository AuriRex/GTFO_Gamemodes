using System;
using Gamemodes.Interfaces;
using Gamemodes.Net;
using Gamemodes.UI;
using Il2CppInterop.Runtime.Attributes;
using UnityEngine;

namespace Gamemodes.Components;

public class TimerHUD : MonoBehaviour
{
    public float Countdown => _countdown;
    public float CountdownInt => _countdownInt;
    public float CountdownMax => _countdownMax;
    
    public float GameTimer => _gameTimer;
    
    private float _countdown = 1;
    private int _countdownInt = 0;
    private int _countdownMax = 60;
    private Func<TimerStyleOverride> _countdownStyleProvider;

    private float _gameTimer = 0;
    private int _gameTimerInt = 0;

    private ITimerHudGraphics _timerHudGraphics = new GameBaseTimerGraphics();
    
    public const string COUNTDOWN_TIMER_MARKER = "[COUNTDOWN]";
    public const string GAME_TIMER_MARKER = "[GAMETIMER]";
    public const string DEFAULT_COUNTDOWN_FORMATTEXT = $"{COUNTDOWN_TIMER_MARKER} left.";

    public string GameTimerFormatText { get; set; } = $"Time elapsed: {GAME_TIMER_MARKER}";
    public float GameTimerHighlightEveryXSeconds { get; set; } = 300;
    public float GameTimerHighlightFoxXSeconds { get; set; } = 10;
    
    private string _countdownTextFormat = DEFAULT_COUNTDOWN_FORMATTEXT;
    private string _messageText = string.Empty;
    
    private bool _messageVisible;
    private bool _gameTimerActive;
    

    [HideFromIl2Cpp]
    public void ResetGameTimer()
    {
        _gameTimer = 0;
        _gameTimerInt = -1;
    }
    
    [HideFromIl2Cpp]
    public void StartGameTimer()
    {
        _gameTimerActive = true;
    }
    
    [HideFromIl2Cpp]
    public void StopGameTimer()
    {
        _gameTimerActive = false;
    }

    [HideFromIl2Cpp]
    public void SetTimerGraphics(ITimerHudGraphics graphics)
    {
        _timerHudGraphics?.Shutdown();
        _timerHudGraphics = graphics;
        _timerHudGraphics?.Initialize();
    }

    [HideFromIl2Cpp]
    public void StartCountdown(int duration, Func<TimerStyleOverride> styleProvider, string formatText = null)
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
        _countdownStyleProvider = GetDefaultGameStartCountdownStyle;
    }

    public void Update()
    {
        if (!NetworkingManager.InLevel)
            return;
        
        if (_gameTimerActive)
        {
            _gameTimer += Time.deltaTime;
        }

        TimerDisplayStyle style = TimerDisplayStyle.Default;
        bool doBlink = false;
        bool showCountdownTimer = _countdown > 0;

        if (showCountdownTimer)
        {
            _countdown -= Time.deltaTime;
        }

        if (_timerHudGraphics == null)
            return;
        
        if (showCountdownTimer)
        {
            DisplayCountdownTimer(ref style, ref doBlink);
        }
        else
        {
            DisplayGameTimer(ref style, ref doBlink);
        }

        if (!_gameTimerActive && !showCountdownTimer)
        {
            if (_messageVisible)
            {
                _timerHudGraphics.SetVisible(false);
                _messageVisible = false;
            }
            return;
        }

        _timerHudGraphics.SetText(_messageText);
        if (showCountdownTimer)
        {
            _timerHudGraphics.SetTimer(_countdown / _countdownMax);
        }
        _timerHudGraphics.SetTimerVisible(showCountdownTimer);
        _timerHudGraphics.SetVisible(true);
        _messageVisible = true;

        // TODO: Rework timer style things
        switch (style)
        {
            default:
            case TimerDisplayStyle.Default:
                SetStyle("CCC", Color.gray, blinking: doBlink);
                break;
            case TimerDisplayStyle.Green:
                SetStyle("0C0", Color.green, blinking: doBlink);
                break;
            case TimerDisplayStyle.Warning:
                SetStyle("FC0", Color.yellow, blinking: doBlink);
                break;
            case TimerDisplayStyle.Red:
                SetStyle("F00", Color.red, blinking: doBlink);
                break;
        }
    }

    [HideFromIl2Cpp]
    private void DisplayGameTimer(ref TimerDisplayStyle style, ref bool doBlink)
    {
        var rounded = Mathf.RoundToInt(_gameTimer);
        
        if (_gameTimerInt % GameTimerHighlightEveryXSeconds < GameTimerHighlightFoxXSeconds)
        {
            style = TimerDisplayStyle.Warning;
            doBlink = true;
        }

        if (rounded == _gameTimerInt)
            return;

        _gameTimerInt = rounded;

        var message = GameTimerFormatText.Replace(GAME_TIMER_MARKER, TimeSpan.FromSeconds(_gameTimerInt).ToString(@"mm\:ss"));

        _messageText = message;
    }

    [HideFromIl2Cpp]
    private void DisplayCountdownTimer(ref TimerDisplayStyle style, ref bool doBlink)
    {
        var rounded = Mathf.RoundToInt(_countdown);

        if (rounded != _countdownInt)
        {
            _countdownInt = rounded;
            _messageText = _countdownTextFormat.Replace(COUNTDOWN_TIMER_MARKER, TimeSpan.FromSeconds(_countdownInt).ToString(@"mm\:ss"));
        }

        var result = _countdownStyleProvider?.Invoke();

        if (!result.HasValue)
        {
            return;
        }

        var styleOverride = result.Value;

        if (!styleOverride.DoOverride)
        {
            return;
        }

        style = styleOverride.Style;
        doBlink = styleOverride.DoBlink;
    }

    [HideFromIl2Cpp]
    private TimerStyleOverride GetDefaultGameStartCountdownStyle()
    {
        var ret = new TimerStyleOverride(false, TimerDisplayStyle.Default, false);

        switch (_countdownInt)
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

    [HideFromIl2Cpp]
    private void SetStyle(string textHex, Color color, bool blinking = false)
    {
        _timerHudGraphics.SetStyle(textHex, color, blinking);
        
        // var msg = GuiManager.InteractionLayer.m_message;
        //
        // msg.m_colorHex = textHex;
        // msg.m_timer.color = new Color(color.r, color.g, color.b, msg.m_timerAlpha);
        // msg.m_blinking = blinking;
    }
}