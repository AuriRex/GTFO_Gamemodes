using Gamemodes.Net;
using Player;

namespace Gamemodes.Core.Voice.Modulators;

public class MuteOtherTeamsModulator : IVoiceVolumeModulator
{
    public PlayerVoiceManager.ApplyVoiceStates ApplyToStates => PlayerVoiceManager.ApplyVoiceStates.InLevel;
    
    public void Modify(PlayerAgent player, ref float volume)
    {
        NetworkingManager.GetPlayerInfo(player.Owner, out var info);

        if (NetworkingManager.LocalPlayerTeam != info.Team)
        {
            volume = 0f;
        }
    }
}