namespace Linxium.SuperShader {
    /// <summary>
    /// Runtime overrides layered on top of Volume settings. Used by transition presets and gameplay code.
    /// </summary>
    public static class PostEffectOverrides {
        public static CRTLookState CRT { get; } = new();
        public static TVStaticState TV { get; } = new();
        public static GlitchState Glitch { get; } = new();

        public static void ResetAll() {
            CRT.Reset();
            TV.Reset();
            Glitch.Reset();
        }

        public sealed class CRTLookState {
            public float ScanlineIntensity;
            public float DistortionAmount;
            public float ScanlineSpeed;
            public float VignetteIntensity;
            public float ChromaticAberration;

            public void Reset() {
                ScanlineIntensity = 0f;
                DistortionAmount = 0f;
                ScanlineSpeed = 0f;
                VignetteIntensity = 0f;
                ChromaticAberration = 0f;
            }
        }

        public sealed class TVStaticState {
            public float NoiseIntensity;
            public float NoiseSpeed;
            public float Monochrome = 1f;

            public void Reset() {
                NoiseIntensity = 0f;
                NoiseSpeed = 0f;
                Monochrome = 1f;
            }
        }

        public sealed class GlitchState {
            public float GlitchStrength;
            public float RollY;
            public float VerticalCollapse;
            public float Flash;

            public void Reset() {
                GlitchStrength = 0f;
                RollY = 0f;
                VerticalCollapse = 0f;
                Flash = 0f;
            }
        }
    }
}
