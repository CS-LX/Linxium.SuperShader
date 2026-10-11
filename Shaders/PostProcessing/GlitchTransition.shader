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
            float _SignalInterference;
            float _SignalTime;

            float SignalHash(float value)
            {
                return frac(sin(value * 127.1) * 43758.5453);
            }

            half3 AddSignalInterference(half3 source, float2 uv)
            {
                // Dense horizontal signal streaks, without moving the source pixels or splitting glyphs.
                float row = floor(uv.y * 320.0);
                float tick = floor(_SignalTime * 12.0);
                float seed = row + tick * 0.37;
                float jitter = SignalHash(seed) - 0.5;
                float edge = smoothstep(0.08, 0.42, abs(uv.x - 0.5) + jitter * 0.12);
                float streak = smoothstep(0.1, 0.7, SignalHash(row * 0.73 + tick * 0.13));
                float scan = pow(saturate(sin(uv.y * 320.0 * 6.2831853) * 0.5 + 0.5), 5.0);
                float fragment = smoothstep(0.35, 0.8,
                    SignalHash(row * 1.31 + floor(uv.x * 38.0 + jitter * 4.0) + tick));
                float density = scan * (0.12 + streak * 0.3 + edge * fragment * 0.7);
                float hue = SignalHash(row * 0.53 + floor(_SignalTime * 2.0));
                half3 tint = lerp(half3(0.42, 0.65, 0.75), half3(0.9, 0.52, 0.35), hue);
                float brightness = max(source.r, max(source.g, source.b));
                float textProtection = 1.0 - smoothstep(0.035, 0.22, brightness);
                return source + tint * density * 0.28 * _SignalInterference * textProtection;
            }

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
                if (_SignalInterference > 0.001)
                    color.rgb = AddSignalInterference(color.rgb, uv);
                color.rgb += _Flash * half3(1.0, 0.9, 0.72);
                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
