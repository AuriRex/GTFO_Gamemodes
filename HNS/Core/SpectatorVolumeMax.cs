using Gamemodes.Core.Voice;
using Gamemodes.Net;
using Player;
using PlayerVoiceManager = Gamemodes.Core.Voice.PlayerVoiceManager;

namespace HNS.Core;

public class SpectatorVolumeMax : IVoiceVolumeModulator
{
    public PlayerVoiceManager.ApplyVoiceStates ApplyToStates => PlayerVoiceManager.ApplyVoiceStates.InLevel;
    public void Modify(PlayerAgent player, ref float volume)
    {
        if (HideAndSeekMode.SimplifyTeam((GMTeam)NetworkingManager.LocalPlayerTeam) != (int)GMTeam.PreGame)
            return;
        
        NetworkingManager.GetPlayerInfo(player.Owner, out var info);

        if (HideAndSeekMode.SimplifyTeam((GMTeam)info.Team) != GMTeam.PreGame)
            return;

        volume = 1f;
    }
}