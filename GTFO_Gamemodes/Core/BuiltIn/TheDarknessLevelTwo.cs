namespace Gamemodes.Core.BuiltIn;

public class TheDarknessLevelTwo : TheDarkness
{
    public override string Description => $"{base.Description}\n\n<b><color=orange>No Navmarkers</color></b>";
    protected override DarknessLevel Level => DarknessLevel.Extreme;
}