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
        if (NetworkingManager.LocalPlayerTeam != (int)GMTeam.PreGameAndOrSpectator)
            return;
        
        NetworkingManager.GetPlayerInfo(player.Owner, out var info);

        if (info.Team != (int)GMTeam.PreGameAndOrSpectator)
            return;

        volume = 1f;
    }
}