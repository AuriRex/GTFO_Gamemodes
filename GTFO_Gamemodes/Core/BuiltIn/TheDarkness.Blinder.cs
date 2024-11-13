using System;
using Player;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace Gamemodes.Core.BuiltIn;

public partial class TheDarkness
{
    private class Blinder : MonoBehaviour
    {
        private LocalPlayerAgent _player;
        private FPSCamera _camera;
        private DarknessLevel _level = DarknessLevel.Hard;
        private bool _isBlind;

        public void Awake()
        {
            _player = GetComponent<LocalPlayerAgent>();
            _camera = _player.FPSCamera;
        }

        private void OnDestroy()
        {
            Reset();
        }

        public void BlindPlayer(DarknessLevel? level = null)
        {
            _level = level ?? _level;
            
            var autoExposure = _camera.postProcessing.m_autoExposure;

            switch (_level)
            {
                case DarknessLevel.Overload:
                    GuiManager.PlayerLayer.SetVisible(false);
                    goto case DarknessLevel.Extreme;
                case DarknessLevel.Extreme:
                    GuiManager.NavMarkerLayer.SetVisible(false);
                    goto case DarknessLevel.Hard;
                default:
                case DarknessLevel.Hard:
                    autoExposure.minLuminance.value = 2000f;
                    autoExposure.maxLuminance.value = 2000f;
                    autoExposure.eyeAdaptation.value = _isBlind ? EyeAdaptation.Fixed : EyeAdaptation.Progressive;
                    break;
            }

            _isBlind = true;
        }

        private void Reset()
        {
            if (_camera == null || _camera.postProcessing == null)
                return;
            
            var autoExposure = _camera.postProcessing.m_autoExposure;

            if (autoExposure == null)
                return;
            
            autoExposure.minLuminance.value = -8f;
            autoExposure.maxLuminance.value = 0f;
            autoExposure.eyeAdaptation.value = EyeAdaptation.Progressive;

            _isBlind = false;
        }
    }
}