using UnityEngine;

namespace Gamemodes.UI;

public record struct TimerStyleOverride(TimerDisplayStyle Style, bool DoBlink = false, Color? CustomColor = null, string CustomColorHex = null);