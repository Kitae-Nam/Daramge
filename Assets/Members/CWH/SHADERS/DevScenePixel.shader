Shader "CWH/Dev Scene Pixel"
{
    Properties
    {
        _PixelHeight ("Pixel Grid Height", Range(90, 540)) = 240
    }

    SubShader
    {
        Tags { "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            Name "Pixelate"
            ZTest Always
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Pixelate
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _PixelHeight;
            CBUFFER_END

            half4 Pixelate(Varyings input) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);
                float2 scale = _BlitScaleBias.xy;
                float2 bias = _BlitScaleBias.zw;
                float2 sourceSize = max(_BlitTexture_TexelSize.zw * abs(scale), 1.0);
                float height = clamp(round(_PixelHeight), 1.0, sourceSize.y);
                float2 grid = max(round(float2(height * sourceSize.x / sourceSize.y, height)), 1.0);
                // 화면 비율에 맞춘 격자의 각 칸에서 색을 한 번만 읽어 선명한 픽셀 블록을 만든다.
                float2 uv = saturate((input.texcoord - bias) / scale);
                float2 cell = min(floor(uv * grid), grid - 1.0);
                float2 sampleUV = (cell + 0.5) / grid * scale + bias;
                return SAMPLE_TEXTURE2D_X_LOD(_BlitTexture, sampler_PointClamp, sampleUV, 0);
            }
            ENDHLSL
        }
    }
}
