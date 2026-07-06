using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
#if UNITY_6000_0_OR_NEWER
using UnityEngine.Rendering.RenderGraphModule;
using UnityEngine.Rendering.RenderGraphModule.Util;
#endif

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
            crtLookShader ??= Shader.Find("Hidden/Linxium/CRTLook");
            tvStaticShader ??= Shader.Find("Hidden/Linxium/TVStatic");
            glitchTransitionShader ??= Shader.Find("Hidden/Linxium/GlitchTransition");

            crtLookMaterial = CreateMaterial(crtLookShader);
            tvStaticMaterial = CreateMaterial(tvStaticShader);
            glitchTransitionMaterial = CreateMaterial(glitchTransitionShader);
            renderPass = new SuperShaderRenderPass(crtLookMaterial, tvStaticMaterial, glitchTransitionMaterial);
        }

        protected override void Dispose(bool disposing) {
            renderPass?.ReleaseResources();
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
            RTHandle tempTexture;

            public bool HasAnyMaterial =>
                crtLookMaterial != null || tvStaticMaterial != null || glitchTransitionMaterial != null;

            public SuperShaderRenderPass(Material crtLook, Material tvStatic, Material glitchTransition) {
                crtLookMaterial = crtLook;
                tvStaticMaterial = tvStatic;
                glitchTransitionMaterial = glitchTransition;
                renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;
                profilingSampler = new ProfilingSampler(nameof(SuperShaderRenderPass));
                ConfigureInput(ScriptableRenderPassInput.Color);
#if UNITY_6000_0_OR_NEWER
                requiresIntermediateTexture = true;
#endif
            }

            public void ReleaseResources() {
                tempTexture?.Release();
            }

            public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData) {
                var descriptor = renderingData.cameraData.cameraTargetDescriptor;
                descriptor.depthBufferBits = 0;
                RenderingUtils.ReAllocateIfNeeded(
                    ref tempTexture,
                    descriptor,
                    FilterMode.Bilinear,
                    TextureWrapMode.Clamp,
                    name: "_SuperShaderTempTexture");
            }

#if UNITY_6000_0_OR_NEWER
            public override void RecordRenderGraph(RenderGraph renderGraph, ContextContainer frameData) {
                var resourceData = frameData.Get<UniversalResourceData>();
                var cameraData = frameData.Get<UniversalCameraData>();

                if (resourceData.isActiveTargetBackBuffer || cameraData.isSceneViewCamera) {
                    return;
                }

                if (!TryCollectActiveMaterials()) {
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
#endif

            public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData) {
                if (renderingData.cameraData.isSceneViewCamera || tempTexture == null) {
                    return;
                }

                if (!TryCollectActiveMaterials()) {
                    return;
                }

                var source = renderingData.cameraData.renderer.cameraColorTargetHandle;
                var cmd = CommandBufferPool.Get();
                using (new ProfilingScope(cmd, profilingSampler)) {
                    RTHandle from = source;
                    RTHandle to = tempTexture;
                    for (int i = 0; i < activeMaterials.Count; i++) {
                        Blitter.BlitCameraTexture(cmd, from, to, activeMaterials[i], 0);
                        (from, to) = (to, from);
                    }

                    if (from != source) {
                        Blitter.BlitCameraTexture(cmd, from, source);
                    }
                }

                context.ExecuteCommandBuffer(cmd);
                CommandBufferPool.Release(cmd);
            }

            bool TryCollectActiveMaterials() {
                activeMaterials.Clear();

                var crtLook = VolumeManager.instance.stack.GetComponent<CRTLookVolume>();
                var tvStatic = VolumeManager.instance.stack.GetComponent<TVStaticVolume>();
                var glitch = VolumeManager.instance.stack.GetComponent<GlitchTransitionVolume>();

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

                return activeMaterials.Count > 0;
            }

            static bool IsCrtLookActive(CRTLookVolume volume) {
                if (HasCrtOverride()) {
                    return true;
                }

                return volume != null && volume.IsActive();
            }

            static bool IsTvStaticActive(TVStaticVolume volume) {
                if (HasTvOverride()) {
                    return true;
                }

                return volume != null && volume.IsActive();
            }

            static bool IsGlitchActive(GlitchTransitionVolume volume) {
                if (HasGlitchOverride()) {
                    return true;
                }

                return volume != null && volume.IsActive();
            }

            static bool HasCrtOverride() =>
                PostEffectOverrides.CRT.ScanlineIntensity > 0f
                || PostEffectOverrides.CRT.DistortionAmount > 0f
                || PostEffectOverrides.CRT.VignetteIntensity > 0f
                || PostEffectOverrides.CRT.ChromaticAberration > 0f
                || PostEffectOverrides.CRT.ScanlineSpeed > 0f;

            static bool HasTvOverride() =>
                PostEffectOverrides.TV.NoiseIntensity > 0f
                || PostEffectOverrides.TV.NoiseSpeed > 0f;

            static bool HasGlitchOverride() =>
                PostEffectOverrides.Glitch.GlitchStrength > 0f
                || PostEffectOverrides.Glitch.RollY > 0f
                || PostEffectOverrides.Glitch.VerticalCollapse > 0f
                || PostEffectOverrides.Glitch.Flash > 0f;

            void SetupCrtLookMaterial(CRTLookVolume volume) {
                float scanline = PostEffectOverrides.CRT.ScanlineIntensity;
                float distortion = PostEffectOverrides.CRT.DistortionAmount;
                float chromatic = PostEffectOverrides.CRT.ChromaticAberration;
                float scanlineSpeed = PostEffectOverrides.CRT.ScanlineSpeed;
                float vignette = PostEffectOverrides.CRT.VignetteIntensity;

                if (volume != null && volume.IsActive()) {
                    scanline += volume.scanlineIntensity.value;
                    distortion += volume.distortionAmount.value;
                    chromatic += volume.chromaticAberration.value;
                    scanlineSpeed += volume.scanlineSpeed.value;
                    vignette += volume.vignetteIntensity.value;
                }

                crtLookMaterial.SetFloat(ScanlineIntensityId, scanline);
                crtLookMaterial.SetFloat(DistortionAmountId, distortion);
                crtLookMaterial.SetFloat(ScanlineSpeedId, scanlineSpeed);
                crtLookMaterial.SetFloat(VignetteIntensityId, vignette);
                crtLookMaterial.SetFloat(ChromaticAberrationId, chromatic);
            }

            void SetupTvStaticMaterial(TVStaticVolume volume) {
                float noise = PostEffectOverrides.TV.NoiseIntensity;
                float speed = PostEffectOverrides.TV.NoiseSpeed;
                float monochrome = PostEffectOverrides.TV.Monochrome;

                if (volume != null && volume.IsActive()) {
                    noise += volume.noiseIntensity.value;
                    speed += volume.noiseSpeed.value;
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

                if (volume != null && volume.IsActive()) {
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
