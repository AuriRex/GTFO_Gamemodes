using UnityEngine;

namespace Gamemodes.Interfaces;

public interface ITimerHudGraphics
{
    void Initialize();
    void SetVisible(bool visible);
    void SetTimerVisible(bool visible);
    void SetText(string text);
    void SetTimer(float value);
    void SetStyle(string textHex, Color color, bool blinking = false);
    void Shutdown();
}