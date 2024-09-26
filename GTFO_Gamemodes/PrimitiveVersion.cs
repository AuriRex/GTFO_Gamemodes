using System;

namespace Gamemodes;

public record class PrimitiveVersion
{
    public int Major { get; private set; }
    public int Minor { get; private set; }
    public int Patch { get; private set; }

    public static readonly PrimitiveVersion None = new(0, 0, 0);

    public PrimitiveVersion(int major, int minor = 0, int patch = 0)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
    }

    public PrimitiveVersion(string versionString)
    {
        var split = versionString.Split('.', 4);

        for (int i = 0; i < split.Length; i++)
        {
            if (!int.TryParse(split[i], out var num))
                throw new ArgumentException($"Invalid version string \"{versionString}\" supplied! ({split[i]})", nameof(versionString));

            switch (i)
            {
                case 0:
                    Major = num;
                    break;
                case 1:
                    Minor = num;
                    break;
                case 2:
                    Patch = num;
                    break;
                default:
                    return;
            }
        }
    }

    public static bool operator >(PrimitiveVersion self, PrimitiveVersion other)
    {
        if (self == other)
            return false;

        if (self.Major > other.Major)
            return true;

        if (self.Major == other.Major)
        {
            if (self.Minor > other.Minor)
                return true;

            if (self.Minor == other.Minor)
            {
                if (self.Patch > other.Patch)
                    return true;
            }
        }

        return false;
    }

    public static bool operator <(PrimitiveVersion self, PrimitiveVersion other)
    {
        return other > self;
    }

    public override string ToString()
    {
        return $"{Major}.{Minor}.{Patch}";
    }

}
