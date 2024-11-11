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
        public void Awake()
        {
            _player = GetComponent<LocalPlayerAgent>();
            _camera = _player.FPSCamera;
            
            BlindPlayer();
        }

        private void OnDestroy()
        {
            Reset();
        }

        public void BlindPlayer()
        {
            var autoExposure = _camera.postProcessing.m_autoExposure;

            autoExposure.minLuminance.value = 2000f;
            autoExposure.maxLuminance.value = 2000f;
            autoExposure.eyeAdaptation.value = EyeAdaptation.Progressive; 
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
        }

        public void ForceBlind()
        {
            var autoExposure = _camera.postProcessing.m_autoExposure;

            autoExposure.minLuminance.value = 2000f;
            autoExposure.maxLuminance.value = 2000f;
            autoExposure.eyeAdaptation.value = EyeAdaptation.Fixed;
        }
    }
}