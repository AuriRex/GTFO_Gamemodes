using BepInEx.Unity.IL2CPP.Utils.Collections;
using HNS.Core;
using HNS.Net;
using Il2CppInterop.Runtime.Attributes;
using Player;
using System;
using System.Collections;
using UnityEngine;

namespace HNS.Components;

public class HideAndSeekGameManager : MonoBehaviour
{
    private float _countdown = 1;
    private int _countdownInt;
    private byte _countdownMax = 60;

    private float _gameTimer = 0;
    private int _gameTimerInt;

    private string _timerText = string.Empty;

    private Blinds _blinds;

    private bool _showFinalTime = false;
    private string _finalTime = "??:??";

    public void StartGame(bool localPlayerIsSeeker, byte blindDuration)
    {
        SetCountdownDuration(blindDuration);
        _gameTimer = 0;
        _gameTimerInt = -1;
        _showFinalTime = false;

        if (localPlayerIsSeeker)
        {
            var blinds = BlindPlayer();
            CoroutineManager.StartCoroutine(UnblindPlayer(blindDuration, blinds).WrapToIl2Cpp());
        }
    }

    [HideFromIl2Cpp]
    internal void StopGame(Session session)
    {
        _finalTime = session.FinalTime.ToString(@"mm\:ss");

        CoroutineManager.StartCoroutine(DisplayFinalTime().WrapToIl2Cpp());

        _blinds?.Dispose();
        _blinds = null;
    }

    [HideFromIl2Cpp]
    private IEnumerator DisplayFinalTime()
    {
        _showFinalTime = true;

        var yielder = new WaitForSeconds(10);
        yield return yielder;

        _showFinalTime = false;
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
    }

    public void SetCountdownDuration(byte duration)
    {
        _countdown = duration;
        _countdownMax = duration;
        _countdownInt = -1;
    }

    public void Awake()
    {

    }

    public void Update()
    {
        if (_showFinalTime)
        {
            GuiManager.InteractionLayer.SetMessage(_finalTime, ePUIMessageStyle.Message, priority: 10);
            GuiManager.InteractionLayer.SetTimerAlphaMul(0f);
            GuiManager.InteractionLayer.MessageTimerVisible = false;
            GuiManager.InteractionLayer.MessageVisible = true;
            SetStyle("0C0", Color.green, blinking: true);
            return;
        }

        if (!NetSessionManager.HasSession)
            return;

        UpdateTimer();
    }

    private void UpdateTimer()
    {
        CStyle style = CStyle.Default;
        bool doBlink = false;
        bool showCountdownTimer = _countdown > 0;
        if (showCountdownTimer)
        {
            _countdown -= Time.deltaTime;

            var rounded = Mathf.RoundToInt(_countdown);

            if (rounded != _countdownInt)
            {
                _countdownInt = rounded;
                _timerText = $"Time until release: {TimeSpan.FromSeconds(_countdownInt).ToString(@"mm\:ss")}";
            }

            if (_countdown <= 10 && _countdown > 0)
            {
                style = CStyle.Red;
                doBlink = true;
            }
            else if (_countdown <= 20 && _countdown > 0)
            {
                style = CStyle.Warning;
            }
        }
        else
        {
            _gameTimer += Time.deltaTime;

            if (_gameTimer <= 10)
            {
                _timerText = $"Seekers have been released!";
                style = CStyle.Green;
                doBlink = true;
            }
            else
            {
                var rounded = Mathf.RoundToInt(_gameTimer);

                if (rounded != _gameTimerInt)
                {
                    _gameTimerInt = rounded;

                    _timerText = $"Hiding for: {TimeSpan.FromSeconds(_gameTimerInt).ToString(@"mm\:ss")}";

                    if (_gameTimerInt % 300 <= 5)
                    {
                        style = CStyle.Warning;
                        doBlink = true;
                    }
                }
            }
        }

        GuiManager.InteractionLayer.SetMessage(_timerText, ePUIMessageStyle.Message, priority: 10);
        GuiManager.InteractionLayer.SetMessageTimer(_countdown / _countdownMax);
        GuiManager.InteractionLayer.SetTimerAlphaMul(showCountdownTimer ? 1f : 0f);
        GuiManager.InteractionLayer.MessageTimerVisible = showCountdownTimer;
        GuiManager.InteractionLayer.MessageVisible = true;

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

    private enum CStyle
    {
        Default,
        Green,
        Warning,
        Red,
    }

    private static void SetStyle(string textHex, Color color, bool blinking = false)
    {
        var msg = GuiManager.InteractionLayer.m_message;

        msg.m_colorHex = textHex;
        msg.m_timer.color = new Color(color.r, color.g, color.b, msg.m_timerAlpha);
        msg.m_blinking = blinking;
    }
}
