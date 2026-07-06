Shader "Linxium/HandDrawnWobble"
{
    Properties
    {
        [MainTexture] _MainTex("Sprite Texture", 2D) = "white" {}
        [MainColor] _Color("Tint", Color) = (1, 1, 1, 1)

        [Header(Wobble Settings)]
        _ShakeSpeed("Wobble Speed (Hz)", Float) = 8.0
        _WobbleScale("Wobble Scale", Float) = 20.0
        _WobbleStrength("Wobble Strength", Float) = 0.005

        [Header(Stencil)]
        _StencilRef("Stencil ID", Int) = 0
        [Enum(UnityEngine.Rendering.CompareFunction)] _StencilComp("Stencil Comparison", Int) = 8
        [Enum(UnityEngine.Rendering.StencilOp)] _StencilPass("Stencil Pass", Int) = 0
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "RenderType" = "Transparent" }

        Cull Off Lighting Off ZWrite Off Blend SrcAlpha OneMinusSrcAlpha

        Stencil
        {
            Ref [_StencilRef]
            Comp [_StencilComp]
            Pass [_StencilPass]
        }

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                half4 _Color;
                float4 _MainTex_ST;
                float _ShakeSpeed;
                float _WobbleScale;
                float _WobbleStrength;
            CBUFFER_END

            float2 SimpleNoise(float2 p)
            {
                return frac(sin(float2(dot(p, float2(127.1, 311.7)), dot(p, float2(269.5, 183.3)))) * 43758.5453);
            }

            Varyings vert(Attributes IN)
            {
                Varyings OUT;

                float snappedTime = floor(_Time.y * _ShakeSpeed);
                float2 noiseSeed = IN.positionOS.xy * _WobbleScale + snappedTime;
                float2 noise = SimpleNoise(noiseSeed) * 2.0 - 1.0;

                float3 displacedPos = IN.positionOS.xyz;
                displacedPos.xy += noise * _WobbleStrength;

                OUT.positionHCS = TransformObjectToHClip(displacedPos);
                OUT.uv = TRANSFORM_TEX(IN.uv, _MainTex);
                OUT.color = IN.color * _Color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                half4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, IN.uv) * IN.color;
                clip(color.a - 0.01);
                return color;
            }
            ENDHLSL
        }
    }
}
