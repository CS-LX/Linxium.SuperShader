using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Linxium.SuperShader {
    [Serializable, VolumeComponentMenu("Linxium/TV Static")]
    public sealed class TVStaticVolume : VolumeComponent, IPostProcessComponent {
        public ClampedFloatParameter noiseIntensity = new(0f, 0f, 1f);
        public ClampedFloatParameter noiseSpeed = new(24f, 0f, 120f);
        public ClampedFloatParameter monochrome = new(1f, 0f, 1f);

        public bool IsActive() => active && noiseIntensity.value > 0f;

        public bool IsTileCompatible() => false;
    }
}
