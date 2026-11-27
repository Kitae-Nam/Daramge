Shader "CWH/Colossus Grid Warning"
{
    Properties
    {
        [HDR] _FillColor("Fill", Color) = (0.96, 0.055, 0.035, 1)
        [HDR] _EdgeColor("Outline", Color) = (1.2, 0.37, 0.12, 1)
        _Opacity("Opacity", Range(0, 1)) = 0
        _Progress("Progress", Range(0, 1)) = 0
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

            CBUFFER_START(UnityPerMaterial)
                half4 _FillColor;
                half4 _EdgeColor;
                half _Opacity;
                half _Progress;
            CBUFFER_END

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 uv : TEXCOORD0;
                float4 exposedEdges : TEXCOORD1;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 uv : TEXCOORD0;
                float4 exposedEdges : TEXCOORD1;
            };

            Varyings Vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = input.uv;
                output.exposedEdges = input.exposedEdges;
                return output;
            }

            half4 Frag(Varyings input) : SV_Target
            {
                float4 distances = float4(input.uv.x, 1 - input.uv.x, input.uv.y, 1 - input.uv.y);
                distances += 1 - input.exposedEdges;
                float edgeDistance = min(min(distances.x, distances.y), min(distances.z, distances.w));
                half border = 1 - smoothstep(0.036, 0.065, edgeDistance);

                // 시작부터 범위를 읽을 수 있게 하고, 기존 텔레그래프처럼 진행에 따라 밝게 채운다.
                float front = lerp(-0.06, 1.06, saturate(_Progress));
                half fill = 1 - smoothstep(-0.045, 0.045, input.uv.z - front);
                half sweep = 1 - smoothstep(0.008, 0.047, abs(input.uv.z - front));
                half accent = max(border, sweep * 0.72);
                half3 color = lerp(_FillColor.rgb * lerp(0.77, 1.17, fill), _EdgeColor.rgb, accent);
                half opacity = _Opacity * max(lerp(0.72, 0.96, fill), accent);
                return half4(color, opacity);
            }
            ENDHLSL
        }
    }
}
