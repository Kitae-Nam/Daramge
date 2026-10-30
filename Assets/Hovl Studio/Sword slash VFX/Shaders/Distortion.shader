Shader "Hovl/Particles/Distortion"
{
	Properties
	{
		_NormalMap("Normal Map", 2D) = "bump" {}
		_Distortionpower("Distortion power", Float) = 0.05
		_InvFade ("Soft Particles Factor", Range(0.01,3.0)) = 1.0
	}

    // Distortion is intentionally disabled because this project uses URP.
    // Keep the shader name, properties and .meta GUID so existing materials stay valid.
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }
        Pass
        {
            Name "DistortionDisabled"
            Tags { "LightMode"="SRPDefaultUnlit" }
            // A disabled distortion layer must not change camera color or depth.
            ColorMask 0
            ZWrite Off
            Cull Off

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float4 Vert(float4 positionOS : POSITION) : SV_POSITION
            {
                // Collapse the layer to a degenerate point to avoid rasterizing particles.
                return float4(0.0, 0.0, 0.0, 1.0);
            }

            half4 Frag() : SV_Target
            {
                // Explicit zero output documents the disabled layer's behavior.
                return half4(0.0, 0.0, 0.0, 0.0);
            }
            ENDHLSL
        }
    }
    Fallback Off
}
