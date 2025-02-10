using Player;

namespace Gamemodes.Core.Voice;

public interface IVoiceVolumeModulator
{
    PlayerVoiceManager.ApplyVoiceStates ApplyToStates { get; }
    void Modify(PlayerAgent player, ref float volume);
}