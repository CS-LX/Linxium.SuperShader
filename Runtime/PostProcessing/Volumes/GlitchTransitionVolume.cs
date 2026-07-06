using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace Linxium.SuperShader {
    [Serializable, VolumeComponentMenu("Linxium/Glitch Transition")]
    public sealed class GlitchTransitionVolume : VolumeComponent, IPostProcessComponent {
        public ClampedFloatParameter glitchStrength = new(0f, 0f, 1f);
        public ClampedFloatParameter rollY = new(0f, 0f, 1f);
        public ClampedFloatParameter verticalCollapse = new(0f, 0f, 1f);
        public ClampedFloatParameter flash = new(0f, 0f, 1f);

        public bool IsActive() =>
            active
            && (glitchStrength.value > 0f
                || rollY.value > 0f
                || verticalCollapse.value > 0f
                || flash.value > 0f);

        public bool IsTileCompatible() => false;
    }
}
