using System;
using UnityEngine;

namespace Gamemodes.Mode;

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
    public string SubTitle => Implementation.SubTitle;
    public string Description => Implementation.Description;
    public Sprite SpriteSmall => Implementation.SpriteSmall;
    public Sprite SpriteLarge => Implementation.SpriteLarge;

    public GamemodeBase Implementation { get; init; }

}
