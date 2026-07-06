using System;
using System.Collections;
using UnityEngine;

namespace Linxium.SuperShader {
    public enum GlitchTransitionKind {
        PowerOff,
        PowerOn
    }

    /// <summary>
    /// Plays CRT-style power on/off transitions using coroutines. No UniTask or DOTween required.
    /// </summary>
    public static class GlitchTransitionPlayer {
        static Coroutine activeRoutine;

        public static bool IsPlaying => activeRoutine != null;

        public static Coroutine Play(GlitchTransitionKind kind, float duration = 0.36f, bool useUnscaledTime = true, Action onComplete = null) {
            Stop();
            activeRoutine = PostEffectCoroutineRunner.Run(kind switch {
                GlitchTransitionKind.PowerOff => PowerOffRoutine(duration, useUnscaledTime, onComplete),
                GlitchTransitionKind.PowerOn => PowerOnRoutine(duration, useUnscaledTime, onComplete),
                _ => PowerOffRoutine(duration, useUnscaledTime, onComplete)
            });
            return activeRoutine;
        }

        public static void Stop() {
            if (activeRoutine == null) {
                return;
            }

            PostEffectCoroutineRunner.Stop(activeRoutine);
            activeRoutine = null;
            PostEffectOverrides.ResetAll();
        }

        static IEnumerator PowerOffRoutine(float duration, bool useUnscaledTime, Action onComplete) {
            PostEffectOverrides.ResetAll();

            float phaseA = duration * 0.35f;
            float phaseB = duration * 0.32f;
            float elapsed = 0f;

            while (elapsed < phaseA) {
                elapsed += Delta(useUnscaledTime);
                float t = elapsed / phaseA;
                PostEffectOverrides.CRT.ScanlineIntensity = Mathf.Lerp(0f, 0.38f, t);
                PostEffectOverrides.CRT.ChromaticAberration = Mathf.Lerp(0f, 0.045f, Smooth01(Mathf.Min(t / 0.4f, 1f)));
                PostEffectOverrides.Glitch.GlitchStrength = Mathf.Lerp(0f, 1f, Smooth01(Mathf.Min(t / 0.42f, 1f)));
                PostEffectOverrides.CRT.DistortionAmount = Mathf.Lerp(0f, 0.14f, Smooth01(Mathf.Min(t / 0.45f, 1f)));
                yield return null;
            }

            elapsed = 0f;
            while (elapsed < phaseB) {
                elapsed += Delta(useUnscaledTime);
                float t = elapsed / phaseB;
                PostEffectOverrides.Glitch.VerticalCollapse = Mathf.Lerp(0f, 0.96f, t);
                PostEffectOverrides.Glitch.RollY = Mathf.Lerp(0f, 0.2f, t);
                PostEffectOverrides.Glitch.Flash = t < 0.1f / 0.32f
                    ? Mathf.Lerp(0f, 0.62f, t / (0.1f / 0.32f))
                    : Mathf.Lerp(0.62f, 0.1f, (t - 0.1f / 0.32f) / (1f - 0.1f / 0.32f));
                yield return null;
            }

            activeRoutine = null;
            onComplete?.Invoke();
        }

        static IEnumerator PowerOnRoutine(float duration, bool useUnscaledTime, Action onComplete) {
            PostEffectOverrides.Glitch.VerticalCollapse = 0.96f;
            PostEffectOverrides.CRT.ScanlineIntensity = 0.42f;
            PostEffectOverrides.CRT.ChromaticAberration = 0.04f;
            PostEffectOverrides.Glitch.GlitchStrength = 0.9f;
            PostEffectOverrides.Glitch.RollY = 0.16f;
            PostEffectOverrides.CRT.DistortionAmount = 0.1f;
            PostEffectOverrides.Glitch.Flash = 0.22f;

            float elapsed = 0f;
            float length = duration * 0.58f;
            while (elapsed < length) {
                elapsed += Delta(useUnscaledTime);
                float t = EaseOut(elapsed / length);
                PostEffectOverrides.Glitch.VerticalCollapse = Mathf.Lerp(0.96f, 0f, t);
                PostEffectOverrides.Glitch.RollY = Mathf.Lerp(0.16f, 0f, Smooth01(Mathf.Min(elapsed / (duration * 0.42f), 1f)));
                PostEffectOverrides.Glitch.GlitchStrength = Mathf.Lerp(0.9f, 0f, Smooth01(Mathf.Min(elapsed / (duration * 0.52f), 1f)));
                PostEffectOverrides.CRT.ChromaticAberration = Mathf.Lerp(0.04f, 0f, Smooth01(Mathf.Min(elapsed / (duration * 0.55f), 1f)));
                PostEffectOverrides.CRT.ScanlineIntensity = Mathf.Lerp(0.42f, 0f, Smooth01(Mathf.Min(elapsed / length, 1f)));
                PostEffectOverrides.CRT.DistortionAmount = Mathf.Lerp(0.1f, 0f, Smooth01(Mathf.Min(elapsed / (duration * 0.5f), 1f)));
                PostEffectOverrides.Glitch.Flash = Mathf.Lerp(0.22f, 0f, Smooth01(Mathf.Min(elapsed / (duration * 0.28f), 1f)));
                yield return null;
            }

            PostEffectOverrides.ResetAll();
            activeRoutine = null;
            onComplete?.Invoke();
        }

        static float Delta(bool useUnscaledTime) => useUnscaledTime ? Time.unscaledDeltaTime : Time.deltaTime;

        static float Smooth01(float t) => t * t * (3f - 2f * t);

        static float EaseOut(float t) {
            t = Mathf.Clamp01(t);
            return 1f - (1f - t) * (1f - t) * (1f - t);
        }
    }
}
