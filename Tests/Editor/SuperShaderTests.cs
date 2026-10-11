using System.Reflection;
using NUnit.Framework;
using UnityEngine;

namespace Linxium.SuperShader.Tests {
    public sealed class SuperShaderTests {
        SuperShaderRendererFeature feature;
        CRTLookVolume crt;
        GlitchTransitionVolume glitch;
        const BindingFlags Hidden = BindingFlags.Instance | BindingFlags.NonPublic;

        [SetUp]
        public void SetUp() {
            PostEffectOverrides.ResetAll();
            crt = ScriptableObject.CreateInstance<CRTLookVolume>();
            glitch = ScriptableObject.CreateInstance<GlitchTransitionVolume>();
        }

        [TearDown]
        public void TearDown() {
            feature?.Dispose();
            Object.DestroyImmediate(feature);
            Object.DestroyImmediate(crt);
            Object.DestroyImmediate(glitch);
            PostEffectOverrides.ResetAll();
        }

        [Test]
        public void DefaultRendererRetainsAdditiveVolumeAndOverrideBehavior() {
            CreateFeature<SuperShaderRendererFeature>();
            Assert.That(Invoke("IsCrtLookActive", crt), Is.False);
            crt.distortionAmount.value = .1f;
            crt.chromaticAberration.value = .01f;
            PostEffectOverrides.CRT.DistortionAmount = .05f;
            PostEffectOverrides.CRT.ChromaticAberration = .02f;
            Invoke("SetupCrtLookMaterial", crt);
            Assert.That(Material("crtLookMaterial").GetFloat("_DistortionAmount"), Is.EqualTo(.15f).Within(.00001f));
            Assert.That(Material("crtLookMaterial").GetFloat("_ChromaticAberration"), Is.EqualTo(.03f).Within(.00001f));
            PostEffectOverrides.ResetAll();
            crt.active = false;
            Assert.That(Invoke("IsCrtLookActive", crt), Is.False);
        }

        [Test]
        public void FixedScreenIgnoresMutablePhysicalSettingsAndSurvivesDisabledVolume() {
            CreateFeature<FixedScreenTestFeature>();
            crt.distortionAmount.value = .45f;
            crt.chromaticAberration.value = .045f;
            PostEffectOverrides.CRT.DistortionAmount = .3f;
            PostEffectOverrides.CRT.ChromaticAberration = .04f;
            foreach (var active in new[] { true, false }) {
                crt.active = active;
                Assert.That(Invoke("IsCrtLookActive", crt), Is.True);
                Invoke("SetupCrtLookMaterial", crt);
                AssertFixedScreen();
            }
            PostEffectOverrides.ResetAll();
            Assert.That(Invoke("IsCrtLookActive", null), Is.True);
            Invoke("SetupCrtLookMaterial", null);
            AssertFixedScreen();
        }

        [Test]
        public void SignalCanBeEnabledFromVolumeOrRuntimeAndClampsTheirSum() {
            CreateFeature<SuperShaderRendererFeature>();
            Assert.That(glitch.IsActive(), Is.False);
            glitch.signalInterference.value = .7f;
            Assert.That(glitch.IsActive(), Is.True);
            Invoke("SetupGlitchMaterial", glitch);
            Assert.That(Material("glitchTransitionMaterial").GetFloat("_SignalInterference"), Is.EqualTo(.7f));
            PostEffectOverrides.Glitch.SignalInterference = .6f;
            Invoke("SetupGlitchMaterial", glitch);
            Assert.That(Material("glitchTransitionMaterial").GetFloat("_SignalInterference"), Is.EqualTo(1f));
            glitch.active = false;
            Invoke("SetupGlitchMaterial", glitch);
            Assert.That(Material("glitchTransitionMaterial").GetFloat("_SignalInterference"), Is.EqualTo(.6f));
            PostEffectOverrides.ResetAll();
            Invoke("SetupGlitchMaterial", glitch);
            Assert.That(Material("glitchTransitionMaterial").GetFloat("_SignalInterference"), Is.Zero);
        }

        void AssertFixedScreen() {
            Assert.That(Material("crtLookMaterial").GetFloat("_DistortionAmount"), Is.EqualTo(.11f));
            Assert.That(Material("crtLookMaterial").GetFloat("_ChromaticAberration"), Is.EqualTo(.023f));
        }

        void CreateFeature<T>() where T : SuperShaderRendererFeature {
            feature = ScriptableObject.CreateInstance<T>();
            // ScriptableRendererFeature.OnEnable also creates materials.
            feature.Dispose();
            feature.Create();
        }

        object Invoke(string method, object volume) {
            var pass = typeof(SuperShaderRendererFeature).GetField("renderPass", Hidden).GetValue(feature);
            return pass.GetType().GetMethod(method, Hidden).Invoke(pass, new[] { volume });
        }

        Material Material(string field) => (Material)typeof(SuperShaderRendererFeature).GetField(field, Hidden).GetValue(feature);
    }

    sealed class FixedScreenTestFeature : SuperShaderRendererFeature {
        protected override CrtScreenProperties? FixedCrtScreen => new CrtScreenProperties(.11f, .023f);
    }
}
