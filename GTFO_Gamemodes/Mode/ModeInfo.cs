using System;
using System.Collections.Generic;

namespace Gamemodes.Mode
{
    public class ModeInfo
    {
        public const int ID_MAX_LENGTH = 25;

        public ModeInfo(string id, string displayName, GamemodeBase implementation)
        {
            if (id.Length > ID_MAX_LENGTH)
                throw new ArgumentException($"Mode ID can't be longer than {ID_MAX_LENGTH} characters!", nameof(id));

            ID = id;
            DisplayName = displayName;
            Implementation = implementation;
        }

        public string ID { get; init; }

        public string DisplayName { get; init; }

        public GamemodeBase Implementation { get; init; }
    }
}
