namespace Linxium.SuperShader {
    /// <summary>
    /// Runtime overrides layered on top of Volume settings. Used by transition presets and gameplay code.
    /// </summary>
    public static class PostEffectOverrides {
        public static CrtLookState Crt { get; } = new();
        public static TvStaticState Static { get; } = new();
        public static GlitchState Glitch { get; } = new();

        public static void ResetAll() {
            Crt.Reset();
            Static.Reset();
            Glitch.Reset();
        }

        public sealed class CrtLookState {
            public float ScanlineIntensity;
            public float DistortionAmount;
            public float ChromaticAberration;

            public void Reset() {
                ScanlineIntensity = 0f;
                DistortionAmount = 0f;
                ChromaticAberration = 0f;
            }
        }

        public sealed class TvStaticState {
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
