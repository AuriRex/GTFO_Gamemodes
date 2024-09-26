using Gamemodes.Mode;
using System;

namespace GamemodesTests;

internal class GamemodeManagerTests
{
    private const string MODE_ID = "my_game_mode";
    private const string LONG_MODE_ID = "this_is_a_very_long_gamemode_id_and_it_should_be_very_much_too_long_so_that_the_thingie_does_the_throwing_an_exception_thgingie";

    private class CustomMode : GamemodeBase
    {
        public override string ID => MODE_ID;

        public override string DisplayName => "Name Here";

        public override ModeSettings Settings => new();
    }

    private class CustomModeLong : GamemodeBase
    {
        public override string ID => LONG_MODE_ID;

        public override string DisplayName => "Name Here";

        public override ModeSettings Settings => new();
    }

    [Test]
    public void DuplicateIDs()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            GamemodeManager.RegisterMode<CustomMode>();
            GamemodeManager.RegisterMode<CustomMode>();
        });
    }

    [Test]
    public void IDTooLong()
    {
        Assert.Throws<ArgumentException>(() =>
        {
            GamemodeManager.RegisterMode<CustomModeLong>();
        });
    }
}
