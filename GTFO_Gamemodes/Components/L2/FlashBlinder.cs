using Gamemodes.Core;
using Gamemodes.Extensions;
using Il2CppInterop.Runtime.Attributes;
using Player;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Gamemodes.Components.L2;

public class FlashBlinder : MonoBehaviour
{
    public static float FLASH_TIME = 1f;
    public static float RECOVERY_TIME = 5f;

    public static float REC_LUMINANCE_MAX = -1000f;
    public static float REC_LUMINANCE_MIN = -2000f;
    
    private float _blindedUntil;
    private BlindState _nextState = BlindState.Fixed;
    
    private LocalPlayerAgent _player;
    private FPSCamera _camera;

    private enum BlindState
    {
        Fixed,
        Recovery,
        End
    }
    
    public static void BlindLocalPlayer()
    {
        if (GameStateManager.CurrentStateName != eGameStateName.InLevel)
            return;
        
        if (!PlayerManager.TryGetLocalPlayerAgent(out var player))
            return;

        player.gameObject.GetOrAddComponent<FlashBlinder>().BlindPlayer();
    }
    
    public void Awake()
    {
        _player = GetComponent<LocalPlayerAgent>();
        _camera = _player.FPSCamera;
    }

    public void Update()
    {
        if (Clock.Time < _blindedUntil)
            return;

        var multi = GamemodeManager.PhotoSensitivityMode ? -1f : 1f;
        
        switch (_nextState)
        {
            case BlindState.Recovery:
                ApplySettings(REC_LUMINANCE_MIN * multi, REC_LUMINANCE_MAX * multi, EyeAdaptation.Progressive);
                _nextState = BlindState.End;
                _blindedUntil = Clock.Time + RECOVERY_TIME;
                return;
            case BlindState.End:
                // Revert to default settings
                ApplySettings();
                Destroy(this);
                break;
            default:
            case BlindState.Fixed:
                return;
        }
    }

    public void BlindPlayer()
    {
        for (var i = 0; i < 20; i++)
        {
            _player.Sound.Post(AK.EVENTS.STINGER);
        }
        
        _blindedUntil = Clock.Time + FLASH_TIME;
        var multi = GamemodeManager.PhotoSensitivityMode ? -1f : 1f;
        ApplySettings(-1500 * multi, -1500 * multi, EyeAdaptation.Fixed);
        _nextState = BlindState.Recovery;
    }

    [HideFromIl2Cpp]
    public void ApplySettings(float minLuminance = -8, float maxLuminance = 0, EyeAdaptation type = EyeAdaptation.Progressive)
    {
        var autoExposure = _camera.postProcessing.m_autoExposure;
        
        autoExposure.minLuminance.value = minLuminance;
        autoExposure.maxLuminance.value = maxLuminance;
        autoExposure.eyeAdaptation.value = type;
    }
}