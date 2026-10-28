Shader "Hovl/Particles/BlendDistort"
{
	Properties
	{
		_MainTex("MainTex", 2D) = "white" {}
		_Noise("Noise", 2D) = "white" {}
		_Flow("Flow", 2D) = "white" {}
		_Mask("Mask", 2D) = "white" {}
		_NormalMap("NormalMap", 2D) = "bump" {}
		_Color("Color", Color) = (0.5,0.5,0.5,1)
		_Distortionpower("Distortion power", Float) = 0
		_SpeedMainTexUVNoiseZW("Speed MainTex U/V + Noise Z/W", Vector) = (0,0,0,0)
		_DistortionSpeedXYPowerZ("Distortion Speed XY Power Z", Vector) = (0,0,0,0)
		_Emission("Emission", Float) = 2
		_Opacity("Opacity", Range( 0 , 3)) = 1
		[Toggle]_Usedepth("Use depth?", Float) = 1
		[Toggle]_Softedges("Soft edges", Float) = 0
		_Depthpower("Depth power", Float) = 1
		[HideInInspector] _texcoord( "", 2D ) = "white" {}
		[HideInInspector] _tex4coord( "", 2D ) = "white" {}
		[HideInInspector] __dirty( "", Int ) = 1
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
