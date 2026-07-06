using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
using UnityEngine.Rendering.Universal;

namespace Linxium.SuperShader {
    public sealed class SuperShaderRendererFeature : ScriptableRendererFeature {
        [SerializeField] Shader crtLookShader;
        [SerializeField] Shader tvStaticShader;
        [SerializeField] Shader glitchTransitionShader;

        Material crtLookMaterial;
        Material tvStaticMaterial;
        Material glitchTransitionMaterial;
        SuperShaderRenderPass renderPass;

        public override void Create() {
            crtLookShader ??= Shader.Find("Hidden/Linxium/CrtLook");
            tvStaticShader ??= Shader.Find("Hidden/Linxium/TvStatic");
            glitchTransitionShader ??= Shader.Find("Hidden/Linxium/GlitchTransition");

            crtLookMaterial = CreateMaterial(crtLookShader);
            tvStaticMaterial = CreateMaterial(tvStaticShader);
            glitchTransitionMaterial = CreateMaterial(glitchTransitionShader);
            renderPass = new SuperShaderRenderPass(crtLookMaterial, tvStaticMaterial, glitchTransitionMaterial);
        }

        protected override void Dispose(bool disposing) {
            renderPass?.Release();
            CoreUtils.Destroy(crtLookMaterial);
            CoreUtils.Destroy(tvStaticMaterial);
            CoreUtils.Destroy(glitchTransitionMaterial);
        }

        public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData) {
            if (renderPass == null || !renderPass.HasAnyMaterial) {
                return;
            }

            renderer.EnqueuePass(renderPass);
        }

        static Material CreateMaterial(Shader shader) => shader != null ? CoreUtils.CreateEngineMaterial(shader) : null;

        sealed class SuperShaderRenderPass : ScriptableRenderPass {
            static readonly int ScanlineIntensityId = Shader.PropertyToID("_ScanlineIntensity");
            static readonly int DistortionAmountId = Shader.PropertyToID("_DistortionAmount");
            static readonly int ScanlineSpeedId = Shader.PropertyToID("_ScanlineSpeed");
            static readonly int VignetteIntensityId = Shader.PropertyToID("_VignetteIntensity");
            static readonly int ChromaticAberrationId = Shader.PropertyToID("_ChromaticAberration");
            static readonly int NoiseIntensityId = Shader.PropertyToID("_NoiseIntensity");
            static readonly int NoiseSpeedId = Shader.PropertyToID("_NoiseSpeed");
            static readonly int MonochromeId = Shader.PropertyToID("_Monochrome");
            static readonly int RollYId = Shader.PropertyToID("_RollY");
            static readonly int GlitchStrengthId = Shader.PropertyToID("_GlitchStrength");
            static readonly int VerticalCollapseId = Shader.PropertyToID("_VerticalCollapse");
            static readonly int FlashId = Shader.PropertyToID("_Flash");

            readonly Material crtLookMaterial;
            readonly Material tvStaticMaterial;
            readonly Material glitchTransitionMaterial;
            readonly List<Material> activeMaterials = new(3);

            public bool HasAnyMaterial =>
                crtLookMaterial != null || tvStaticMaterial != null || glitchTransitionMaterial != null;

            public SuperShaderRenderPass(Material crtLook, Material tvStatic, Material glitchTransition) {
                crtLookMaterial = crtLook;
                tvStaticMaterial = tvStatic;
                glitchTransitionMaterial = glitchTransition;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                profilingSampler = new ProfilingSampler(nameof(SuperShaderRenderPass));
                ConfigureInput(ScriptableRenderPassInput.Color);
                requiresIntermediateTexture = true;
            }

            public void Release() { }

            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.isActiveTargetBackBuffer || cameraData.isSceneViewCamera) {
                    return;
                }

                var crtLook = VolumeManager.instance.stack.GetComponent<CrtLookVolume>();
                var tvStatic = VolumeManager.instance.stack.GetComponent<TvStaticVolume>();
                var glitch = VolumeManager.instance.stack.GetComponent<GlitchTransitionVolume>();

                activeMaterials.Clear();
                if (IsGlitchActive(glitch) && glitchTransitionMaterial != null) {
                    SetupGlitchMaterial(glitch);
                    activeMaterials.Add(glitchTransitionMaterial);
                }

                if (IsCrtLookActive(crtLook) && crtLookMaterial != null) {
                    SetupCrtLookMaterial(crtLook);
                    activeMaterials.Add(crtLookMaterial);
                }

                if (IsTvStaticActive(tvStatic) && tvStaticMaterial != null) {
                    SetupTvStaticMaterial(tvStatic);
                    activeMaterials.Add(tvStaticMaterial);
                }

                if (activeMaterials.Count == 0) {
                    return;
                }

                var source = resourceData.activeColorTexture;
                for (int i = 0; i < activeMaterials.Count; i++) {
                    var destinationDesc = renderGraph.GetTextureDesc(source);
                    destinationDesc.name = $"SuperShaderPass{i}";
                    destinationDesc.clearBuffer = false;
                    TextureHandle destination = renderGraph.CreateTexture(destinationDesc);

                    var blitParams = new RenderGraphUtils.BlitMaterialParameters(source, destination, activeMaterials[i], 0);
                    renderGraph.AddBlitPass(blitParams, passName: $"SuperShader Blit {i}");

                    source = destination;
                }

                resourceData.cameraColor = source;
            }

            static bool IsCrtLookActive(CrtLookVolume volume) {
                if (volume == null || !volume.active) {
                    return HasCrtOverride();
                }

                return volume.IsActive() || HasCrtOverride();
            }

            static bool IsTvStaticActive(TvStaticVolume volume) {
                if (volume == null || !volume.active) {
                    return PostEffectOverrides.Static.NoiseIntensity > 0f;
                }

                return volume.IsActive() || PostEffectOverrides.Static.NoiseIntensity > 0f;
            }

            static bool IsGlitchActive(GlitchTransitionVolume volume) {
                if (volume == null || !volume.active) {
                    return HasGlitchOverride();
                }

                return volume.IsActive() || HasGlitchOverride();
            }

            static bool HasCrtOverride() =>
                PostEffectOverrides.Crt.ScanlineIntensity > 0f
                || PostEffectOverrides.Crt.DistortionAmount > 0f
                || PostEffectOverrides.Crt.ChromaticAberration > 0f;

            static bool HasGlitchOverride() =>
                PostEffectOverrides.Glitch.GlitchStrength > 0f
                || PostEffectOverrides.Glitch.RollY > 0f
                || PostEffectOverrides.Glitch.VerticalCollapse > 0f
                || PostEffectOverrides.Glitch.Flash > 0f;

            void SetupCrtLookMaterial(CrtLookVolume volume) {
                float scanline = PostEffectOverrides.Crt.ScanlineIntensity;
                float distortion = PostEffectOverrides.Crt.DistortionAmount;
                float chromatic = PostEffectOverrides.Crt.ChromaticAberration;
                float scanlineSpeed = 8f;
                float vignette = 0.12f;

                if (volume != null && volume.active) {
                    scanline += volume.scanlineIntensity.value;
                    distortion += volume.distortionAmount.value;
                    chromatic += volume.chromaticAberration.value;
                    scanlineSpeed = volume.scanlineSpeed.value;
                    vignette = volume.vignetteIntensity.value;
                }

                crtLookMaterial.SetFloat(ScanlineIntensityId, scanline);
                crtLookMaterial.SetFloat(DistortionAmountId, distortion);
                crtLookMaterial.SetFloat(ScanlineSpeedId, scanlineSpeed);
                crtLookMaterial.SetFloat(VignetteIntensityId, vignette);
                crtLookMaterial.SetFloat(ChromaticAberrationId, chromatic);
            }

            void SetupTvStaticMaterial(TvStaticVolume volume) {
                float noise = PostEffectOverrides.Static.NoiseIntensity;
                float speed = 24f;
                float monochrome = 1f;

                if (volume != null && volume.active) {
                    noise += volume.noiseIntensity.value;
                    speed = volume.noiseSpeed.value;
                    monochrome = volume.monochrome.value;
                }

                tvStaticMaterial.SetFloat(NoiseIntensityId, noise);
                tvStaticMaterial.SetFloat(NoiseSpeedId, speed);
                tvStaticMaterial.SetFloat(MonochromeId, monochrome);
            }

            void SetupGlitchMaterial(GlitchTransitionVolume volume) {
                float glitch = PostEffectOverrides.Glitch.GlitchStrength;
                float roll = PostEffectOverrides.Glitch.RollY;
                float collapse = PostEffectOverrides.Glitch.VerticalCollapse;
                float flash = PostEffectOverrides.Glitch.Flash;

                if (volume != null && volume.active) {
                    glitch += volume.glitchStrength.value;
                    roll += volume.rollY.value;
                    collapse += volume.verticalCollapse.value;
                    flash += volume.flash.value;
                }

                glitchTransitionMaterial.SetFloat(GlitchStrengthId, glitch);
                glitchTransitionMaterial.SetFloat(RollYId, roll);
                glitchTransitionMaterial.SetFloat(VerticalCollapseId, collapse);
                glitchTransitionMaterial.SetFloat(FlashId, flash);
            }
        }
    }
}
