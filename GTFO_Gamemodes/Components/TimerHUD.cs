using System;
using Gamemodes.Interfaces;
using Gamemodes.Net;
using Gamemodes.UI;
using Il2CppInterop.Runtime.Attributes;
using JetBrains.Annotations;
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
    private Func<TimerStyleOverride?> _countdownStyleProvider;

    private float _gameTimer = 0;
    private int _gameTimerInt = 0;

    private ITimerHudGraphics _timerHudGraphics = new GameBaseTimerGraphics();
    
    public const string COUNTDOWN_TIMER_MARKER = "[COUNTDOWN]";
    public const string GAME_TIMER_MARKER = "[GAMETIMER]";
    public const string DEFAULT_COUNTDOWN_FORMATTEXT = $"{COUNTDOWN_TIMER_MARKER}";

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
    public void StartCountdown(int duration, string formatText = null, Func<TimerStyleOverride?> styleProvider = null)
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

        TimerStyleOverride? styleOverride = null;
        bool showCountdownTimer = _countdown > 0;

        if (showCountdownTimer)
        {
            _countdown -= Time.deltaTime;
        }

        if (_timerHudGraphics == null)
            return;
        
        if (showCountdownTimer)
        {
            DisplayCountdownTimer(ref styleOverride);
        }
        else
        {
            DisplayGameTimer(ref styleOverride);
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

        TimerStyleOverride style = TSO_DEFAULT;
        if (styleOverride.HasValue)
        {
            style = styleOverride.Value;
        }

        switch (style.Style)
        {
            default:
            case TimerDisplayStyle.Default:
                SetStyle("CCC", Color.gray, style.DoBlink);
                break;
            case TimerDisplayStyle.Green:
                SetStyle("0C0", Color.green, style.DoBlink);
                break;
            case TimerDisplayStyle.Warning:
                SetStyle("FC0", Color.yellow, style.DoBlink);
                break;
            case TimerDisplayStyle.Red:
                SetStyle("F00", Color.red, style.DoBlink);
                break;
            case TimerDisplayStyle.Custom:
                SetStyle(style.CustomColorHex, style.CustomColor ?? Color.gray, style.DoBlink);
                break;
        }
    }

    [HideFromIl2Cpp]
    private void DisplayGameTimer(ref TimerStyleOverride? styleOverride)
    {
        var rounded = Mathf.RoundToInt(_gameTimer);
        
        if (_gameTimerInt % GameTimerHighlightEveryXSeconds < GameTimerHighlightFoxXSeconds)
        {
            styleOverride = TSO_ORANGE_WARNING_BLINKING;
        }

        if (rounded == _gameTimerInt)
            return;

        _gameTimerInt = rounded;

        var message = GameTimerFormatText.Replace(GAME_TIMER_MARKER, TimeSpan.FromSeconds(_gameTimerInt).ToString(@"mm\:ss"));

        _messageText = message;
    }

    [HideFromIl2Cpp]
    private void DisplayCountdownTimer(ref TimerStyleOverride? styleOverride)
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

        styleOverride = result.Value;
    }

    public static readonly TimerStyleOverride TSO_DEFAULT = new(TimerDisplayStyle.Default);
    public static readonly TimerStyleOverride TSO_GREEN = new(TimerDisplayStyle.Green);
    public static readonly TimerStyleOverride TSO_GREEN_BLINKING = new(TimerDisplayStyle.Green, DoBlink: true);
    public static readonly TimerStyleOverride TSO_ORANGE_WARNING = new(TimerDisplayStyle.Warning);
    public static readonly TimerStyleOverride TSO_ORANGE_WARNING_BLINKING = new(TimerDisplayStyle.Warning, DoBlink: true);
    public static readonly TimerStyleOverride TSO_RED_WARNING = new(TimerDisplayStyle.Red);
    public static readonly TimerStyleOverride TSO_RED_WARNING_BLINKING = new(TimerDisplayStyle.Red, DoBlink: true);
    
    [HideFromIl2Cpp]
    private TimerStyleOverride? GetDefaultGameStartCountdownStyle()
    {
        return _countdownInt switch
        {
            <= 10 and > 0 => TSO_RED_WARNING_BLINKING,
            <= 20 and > 10 => TSO_ORANGE_WARNING,
            _ => TSO_DEFAULT
        };
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