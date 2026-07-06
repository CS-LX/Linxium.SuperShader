Shader "Hidden/Linxium/GlitchTransition"
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
            Name "GlitchTransition"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _RollY;
            float _GlitchStrength;
            float _VerticalCollapse;
            float _Flash;

            float2 ApplyGlitchUV(float2 uv)
            {
                if (_VerticalCollapse > 0.001)
                {
                    float collapseScale = max(1.0 - _VerticalCollapse * 0.985, 0.015);
                    uv.y = (uv.y - 0.5) / collapseScale + 0.5;
                }

                if (_GlitchStrength > 0.001)
                {
                    float band = floor(uv.y * 56.0);
                    float noise = frac(sin(band * 127.1 + _Time.y * 72.0) * 43758.5453);
                    uv.x += (noise - 0.5) * _GlitchStrength * 0.075;
                }

                if (_RollY > 0.001)
                {
                    uv.y = frac(uv.y + _RollY);
                }

                return uv;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                float2 uv = ApplyGlitchUV(input.texcoord);
                if (uv.x < 0.0 || uv.x > 1.0 || uv.y < 0.0 || uv.y > 1.0)
                {
                    return half4(0.0, 0.0, 0.0, 1.0);
                }

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, uv);
                color.rgb += _Flash * half3(1.0, 0.9, 0.72);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
