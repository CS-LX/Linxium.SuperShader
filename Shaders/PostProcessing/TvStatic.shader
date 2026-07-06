Shader "Hidden/Linxium/TvStatic"
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
            Name "TvStatic"
            ZWrite Off
            ZTest Always
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            float _NoiseIntensity;
            float _NoiseSpeed;
            float _Monochrome;

            float RandomNoise(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            half4 Frag(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                half4 color = SAMPLE_TEXTURE2D_X(_BlitTexture, sampler_LinearClamp, input.texcoord);

                float2 noiseUv = input.texcoord * _ScreenParams.xy;
                float timeOffset = _Time.y * _NoiseSpeed;
                float noise = RandomNoise(floor(noiseUv) + timeOffset);

                half3 snow = lerp(half3(noise, noise, noise), half3(noise, 1.0 - noise, noise * 0.5), _Monochrome);
                color.rgb = lerp(color.rgb, snow, _NoiseIntensity);

                return color;
            }
            ENDHLSL
        }
    }

    Fallback Off
}
