// 장판 표시는 전용 URP 비조명 셰이더로 분리하여 피해 로직이 머티리얼 속성에 의존하지 않게 한다.
Shader "CWH/Boss Dash Scorch"
{
    Properties
    {
        [HDR] _BaseColor("Color", Color) = (1, 0.15, 0.04, 0.55)
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            Cull Off
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            struct Attributes { float4 positionOS : POSITION; float2 uv : TEXCOORD0; };
            struct Varyings { float4 positionCS : SV_POSITION; float2 uv : TEXCOORD0; };
            CBUFFER_START(UnityPerMaterial)
                half4 _BaseColor;
            CBUFFER_END
            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                return output;
            }
            half4 Frag(Varyings input) : SV_Target
            {
                half4 color = _BaseColor;
                color.a *= saturate(min(input.uv.x, 1 - input.uv.x) * 8);
                return color;
            }
            ENDHLSL
        }
    }
}
