Shader "Hidden/Linxium/CRTLook"
{
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "Opaque"
        }

        Pass
        {
            Name "CRTLook"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _ScanlineIntensity;
            float _DistortionAmount;
            float _ScanlineSpeed;
            float _VignetteIntensity;
            float _ChromaticAberration;

            float2 DistortUV(float2 uv)
            {
                float2 centered = uv - 0.5;
                float radiusSquared = dot(centered, centered);
                return uv + centered * radiusSquared * _DistortionAmount;
            }

            float SampleScanline(float2 uv)
            {
                float pixelY = uv.y * _ScreenParams.y;
                float scrollOffset = _Time.y * _ScanlineSpeed * 0.018;
                float scrolledUvY = uv.y + scrollOffset;
                float sineLine = sin(scrolledUvY * _ScreenParams.y * 1.85) * 0.5 + 0.5;

                float rollCoord = frac(pixelY * 0.09 - _Time.y * _ScanlineSpeed * 0.55);
                float bandLine = smoothstep(0.78, 0.92, rollCoord);

                return lerp(sineLine, bandLine, 0.3);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = DistortUV(input.texcoord);
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                half4 color;
                if (_ChromaticAberration > 0.0)
                {
                    float2 centered = uv - 0.5;
                    float radiusSq = dot(centered, centered);
                    float aberration = radiusSq * radiusSq * _ChromaticAberration;
                    float2 dir = normalize(centered + 1e-5);
                    float2 offset = dir * aberration * (_ScreenParams.x * rcp(_ScreenParams.xy));
                    half r = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv + offset).r;
                    half g = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv).g;
                    half b = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv - offset).b;
                    color = half4(r, g, b, 1.0);
                }
                else
                {
                    color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                }

                float scanline = SampleScanline(uv);
                color.rgb *= lerp(1.0, scanline, _ScanlineIntensity);

                float2 centered = uv - 0.5;
                float vignette = 1.0 - dot(centered, centered) * _VignetteIntensity * 4.0;
                color.rgb *= saturate(vignette);

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
