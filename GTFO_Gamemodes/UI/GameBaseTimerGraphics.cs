using Gamemodes.Interfaces;
using UnityEngine;

namespace Gamemodes.UI;

public class GameBaseTimerGraphics : ITimerHudGraphics
{
    public void SetText(string text)
    {
        GuiManager.InteractionLayer.SetMessage(text, ePUIMessageStyle.Message, priority: 10);
    }

    public void SetTimer(float value)
    {
        GuiManager.InteractionLayer.SetMessageTimer(value);
    }

    public void SetStyle(string textHex, Color color, bool blinking = false)
    {
        var msg = GuiManager.InteractionLayer.m_message;

        msg.m_colorHex = textHex;
        msg.m_timer.color = new Color(color.r, color.g, color.b, msg.m_timerAlpha);
        msg.m_blinking = blinking;
    }

    public void Shutdown()
    {
        SetTimerVisible(false);
        SetVisible(false);
    }

    public void Initialize()
    {
        
    }

    public void SetVisible(bool visible)
    {
        GuiManager.InteractionLayer.MessageVisible = visible;
    }

    public void SetTimerVisible(bool visible)
    {
        GuiManager.InteractionLayer.SetTimerAlphaMul(visible ? 1f : 0f);
        GuiManager.InteractionLayer.MessageTimerVisible = visible;
    }
}