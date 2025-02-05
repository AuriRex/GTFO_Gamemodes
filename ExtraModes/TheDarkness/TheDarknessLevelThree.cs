namespace ExtraModes.TheDarkness;

public class TheDarknessLevelThree : TheDarknessLevelTwo
{
    public override string Description => $"{base.Description}\n<b><#F00>No HUD at all.</color></b>";
    protected override DarknessLevel Level => DarknessLevel.Overload;
}