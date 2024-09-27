using BepInEx.Unity.IL2CPP.Utils.Collections;
using HNS.Core;
using HNS.Net;
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

    public void StartGame(bool localPlayerIsSeeker, byte blindDuration)
    {
        SetCountdownDuration(blindDuration);
        _gameTimer = 0;
        _gameTimerInt = -1;

        if (localPlayerIsSeeker)
        {
            BlindPlayer();
            CoroutineManager.StartCoroutine(UnblindPlayer(blindDuration).WrapToIl2Cpp());
        }
    }

    private void BlindPlayer()
    {
        if (!PlayerManager.TryGetLocalPlayerAgent(out var localPlayer))
            return;

        _blinds = new(localPlayer.TryCast<LocalPlayerAgent>());
    }

    private IEnumerator UnblindPlayer(byte blindDuration)
    {
        var yielder = new WaitForSeconds(blindDuration);
        yield return yielder;

        _blinds?.Dispose();
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
        if (!NetSessionManager.HasSession)
            return;

        UpdateTimer();
    }

    private void UpdateTimer()
    {
        CStyle style = CStyle.Default;
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
                style = CStyle.RedBlink;
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
                style = CStyle.GreenBlink;
            }
            else
            {
                var rounded = Mathf.RoundToInt(_gameTimer);

                if (rounded != _gameTimerInt)
                {
                    _gameTimerInt = rounded;
                    _timerText = $"Hiding for: {TimeSpan.FromSeconds(_gameTimerInt).ToString(@"mm\:ss")}";
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
                SetStyle("CCC", Color.gray);
                break;
            case CStyle.Warning:
                SetStyle("FC0", Color.yellow);
                break;
            case CStyle.GreenBlink:
                SetStyle("0C0", Color.green, blinking: true);
                break;
            case CStyle.RedBlink:
                SetStyle("F00", Color.red, blinking: true);
                break;
        }
    }

    private enum CStyle
    {
        RedBlink,
        GreenBlink,
        Warning,
        Default
    }

    private static void SetStyle(string textHex, Color color, bool blinking = false)
    {
        var msg = GuiManager.InteractionLayer.m_message;

        msg.m_colorHex = textHex;
        msg.m_timer.color = new Color(color.r, color.g, color.b, msg.m_timerAlpha);
        msg.m_blinking = blinking;
    }
}
