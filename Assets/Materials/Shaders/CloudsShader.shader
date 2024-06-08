/** 
 * Postprocessing (fullscreen) Shader to display volumetric clouds for malaised hexes
 * Works in multiple steps:
 * - take screenspace rectangle pixel to uv position 
 * - Raycast into the world to find overlap with cloud layer (box)
 * - Check if the position in that box is for a hexagon (map world space to hex space)
 * - Check if hexagon is malaised (MalaiseBuffer, bitbanged)
 * - Raymarch through the box for a malaised hexes
 * - check cloud density (aka layered noise) for each ray step
 *
 */
Shader "Custom/Clouds"
{
    Properties
    {
        _NoiseTex ("NoiseTex", 3D) = "white" {}

        [Header(Clouds)][Space]
        _StepAmount ("Step Amount", Range(1, 25)) = 1
        _CloudHeightMin("Height Min", Float) = 10
        _CloudHeightMax("Height Max", Float) = 15
        _CloudCutoff ("Cutoff", Range(0, 1)) = 0.5
        _CloudDensityMultiplier("Density Multiplier", Range(1, 100)) = 50
        _CloudColor("Color", Color) = (0, 0, 0, 1)
        _NoiseWeights("Noise Weights", Vector) = (1, 0, 0, 0)
        _WindSpeed("Wind Speed", Range(0.001, 0.1)) = 0.01
        _WindCrossMulti("Wind Cross Multi", Range(1, 20)) = 20
        
        [Header(Light)][Space]
        _LightStepAmount ("Step Amount", Range(1, 15)) = 1
        _LightAbsorptionTowardsSun ("Absorption Sun", Range(0, 2)) = 0.5
        _LightAbsorptionThroughCloud ("Absorption Through Cloud", Range(0, 2)) = 0.5
        _DarknessThreshold("Darkness Threshold", Range(0, 1)) = 0.5
        _PhaseParams("Phase Params", Vector) = (0, 0, 0, 0)
        
        [Header(Shadows)][Space]
        _ShadowOffset ("Offset", Vector) = (0, 0, 0, 0)
    }


    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Assets/Materials/Shaders/Util/CloudUtil.cginc"

    
    struct Attributes {
        uint vertexID : SV_VertexID;
    };


    CBUFFER_START(UnityPerMaterial)
    
        float4 _NoiseTex_ST;
        float4 _NoiseTex_TexelSize;

    CBUFFER_END
    
    TEXTURE2D_X(_CameraColorTexture);
    SAMPLER(sampler_CameraColorTexture);


    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" "Queue"="Transparent"}
        LOD 100

        Cull Off

        Pass
        {
            Name "Cloud Lighting"
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            
            #pragma multi_compile __ ENABLE_LIGHT_PASS

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma enable_d3d11_debug_symbols //renderdoc
            
            Varyings vert(Attributes i) {
				Varyings OUT;
                
                /** create a fullscreen rect out of thin air */
                float4 pos = GetFullScreenTriangleVertexPosition(i.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(i.vertexID);
				OUT.positionCS = pos;
                // ignore cause we will calculate by UV (prolly improvable)
                OUT.positionWS = 0;
				OUT.uv = uv;
				return OUT;
			}

            half4 frag(Varyings input) : SV_Target
            { 
                float2 UV = (input.uv - 0.5) * 2;
                float3 PositionWorld = _CameraPos.xyz + UV.x * _CameraRight.xyz + UV.y * _CameraUp.xyz + 100 * -_CameraForward.xyz;
    
                float4 OriginalColor = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, input.uv);
                return GetCloudColorForPixel(PositionWorld, _NoiseTex_ST, _NoiseTex_TexelSize, OriginalColor);
            }
            ENDHLSL
        } // end Pass


    } // end SubShader

    
}
