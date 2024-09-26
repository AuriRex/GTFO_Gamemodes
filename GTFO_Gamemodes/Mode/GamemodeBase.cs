using Gamemodes.Net;
using System.Collections.Generic;

namespace Gamemodes.Mode;

public abstract class GamemodeBase
{
    public abstract string ID { get; }

    public abstract string DisplayName { get; }

    public abstract ModeSettings Settings { get; }

    public IEnumerable<PlayerWrapper> ValidPlayers => NetworkingManager.AllValidPlayers;
    public IEnumerable<PlayerWrapper> Spectators => NetworkingManager.Spectators;

    public virtual void Init()
    {

    }

    public virtual void Enable()
    {

    }

    public virtual void Disable()
    {

    }

    public virtual void OnPlayerCountChanged()
    {

    }


}
