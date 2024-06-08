/** 
 * Fullscreen but not postprocessing shader!
 * Since postprocessing creates shadows that kinda overlap with the clouds, we need an actual world shadow
 * Create a moving quad, project the cloud generation onto it and clip everything that does not produce a clouds
 * Also only run the cloud scripts a fraction of the actual cloud amount to reduce performance
 */
Shader "Custom/CloudsShadow"
{
    Properties
    { 
        _NoiseTex ("NoiseTex", 3D) = "white" {}

        // make sure these are the exact same values as in the cloud shader!
        [Header(Clouds)][Space]
        _StepAmount ("Step Amount", Range(1, 25)) = 1
        _CloudHeightMin("Height Min", Float) = 10
        _CloudHeightMax("Height Max", Float) = 15
        _CloudCutoff ("Cutoff", Range(0, 1)) = 0.5
        _CloudDensityMultiplier("Density Multiplier", Range(1, 100)) = 50
        _CloudColor("Color", Color) = (0, 0, 0, 1)
        _NoiseWeights("Noise Weights", Vector) = (1, 0, 0, 0)
        _WindSpeed("Wind Speed", Range(0.001, 0.1)) = 0.01
        _WindCrossMulti("Wind Cross Multi", Range(1, 20)) = 1
        
        // hidden cause not necessary to adapt for shadows (ie no light)
        // but definition is important for clouds include
        [Header(Light)][Space]
        [HideInInspector]_LightStepAmount ("Step Amount", Range(1, 15)) = 1
        [HideInInspector]_LightAbsorptionTowardsSun ("Absorption Sun", Range(0, 2)) = 0.5
        [HideInInspector]_LightAbsorptionThroughCloud ("Absorption Through Cloud", Range(0, 2)) = 0.5
        [HideInInspector]_DarknessThreshold("Darkness Threshold", Range(0, 1)) = 0.5
        [HideInInspector]_PhaseParams("Phase Params", Vector) = (0, 0, 0, 0)
        
        [Header(Shadows)][Space]
        _UVScale ("UV Scale", Float) = 1
        _AlphaScale ("Alpha Scale", Range(0, 10)) = 1
    
    }

    

    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl" 

    CBUFFER_START(UnityPerMaterial)
    
        float4 _NoiseTex_ST;
        float4 _NoiseTex_TexelSize;

        float _UVScale;
        float _AlphaScale;

    CBUFFER_END

    ENDHLSL

    SubShader
    {
        Tags {"RenderPipeline" = "UniversalPipeline"}

         Pass
        {
            Tags {"LightMode" = "ShadowCaster"  }
        
            LOD 100

            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl" 
            #include "Assets/Materials/Shaders/Util/CloudUtil.cginc"
            
            struct Attributes {
	            float4 vertex 	: POSITION;

            };

            struct v2f {
	            //float4 positionCS 	: SV_POSITION;
                float3 positionWS : TEXCOORD1;
	            float2 uv		: TEXCOORD0;
            };

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            v2f vert (Attributes v, out float4 positionCS : SV_POSITION)
            {
                v2f o;
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(v.vertex.xyz);
				positionCS = VertexInputs.positionCS;
                o.positionWS = VertexInputs.positionWS;
				o.uv = ComputeScreenPos (VertexInputs.positionCS);
                return o;
            }

            //float4 screenPos : VPOS / SV_POSITION
            float4 frag(v2f i, float4 screenPos : VPOS) : SV_Target
            {
                // is uninitialized in game view, so shadows will be hidden, debug remove
                clip(_bIsEnabled <= 0 ? -1 : 0);

                float4 Cloud = GetCloudColorForPixel(i.positionWS, _NoiseTex_ST, _NoiseTex_TexelSize, 0);
                
                clip(dither(screenPos.xy * _UVScale, Cloud.r * _AlphaScale) + 0.001);
                return 1;
            }
            ENDHLSL
        }

    }
}
