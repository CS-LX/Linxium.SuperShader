using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Linxium.SuperShader {
    [Serializable, VolumeComponentMenu("Linxium/CRT Look")]
    public sealed class CRTLookVolume : VolumeComponent, IPostProcessComponent {
        public ClampedFloatParameter scanlineIntensity = new(0.32f, 0f, 1f);
        public ClampedFloatParameter distortionAmount = new(0.18f, 0f, 0.5f);
        public ClampedFloatParameter scanlineSpeed = new(8f, 0f, 12f);
        public ClampedFloatParameter vignetteIntensity = new(0.12f, 0f, 0.5f);
        public ClampedFloatParameter chromaticAberration = new(0.005f, 0f, 0.05f);

        public bool IsActive() =>
            active
            && (scanlineIntensity.value > 0f
                || distortionAmount.value > 0f
                || vignetteIntensity.value > 0f
                || chromaticAberration.value > 0f);

        public bool IsTileCompatible() => false;
    }
}
