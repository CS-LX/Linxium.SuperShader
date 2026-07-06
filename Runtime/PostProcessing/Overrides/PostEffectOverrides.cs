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
            public float ChromaticAberration;

            public void Reset() {
                ScanlineIntensity = 0f;
                DistortionAmount = 0f;
                ChromaticAberration = 0f;
            }
        }

        public sealed class TVStaticState {
            public float NoiseIntensity;

            public void Reset() {
                NoiseIntensity = 0f;
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
