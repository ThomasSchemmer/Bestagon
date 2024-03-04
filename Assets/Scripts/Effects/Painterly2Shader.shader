/*
* Test shader to implement the painterly effect from https://www.youtube.com/watch?v=UfSw6428bcc
* Will be integrated into the hexagonshader
*/

Shader "Custom/Painterly2Shader"
{
    Properties
    {
        _Scale ("Scale", Float) = 1
        _VoronoiMix("VoronoiMix", Range(0, 0.3)) = 0.1
        
    }

    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
    CBUFFER_START(UnityPerMaterial)
        float _Scale;
        float _VoronoiMix;
    CBUFFER_END

    ENDHLSL
    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass
        {
            Tags {"LightMode" = "UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"  
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // for ambient light
            #include "Assets/Materials/Shaders/Util/Util.cginc" //for snoise, voronoy

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 object : TEXCOORD1;
                float3 normal : NORMAL;
            };

            v2f vert (appdata v)
            {
                v2f o;
                
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(v.vertex.xyz);
                VertexNormalInputs NormalInputs = GetVertexNormalInputs(v.normal);
                o.vertex = VertexInputs.positionCS;
                o.object = v.vertex.xyz;
                o.normal = NormalInputs.normalWS;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 col = half4(1, 1, 1, 1);
                float3 normNoise = ssnoise(i.object, 5, 0, 3, 2);
                float3 norm = i.normal * (1 - _VoronoiMix) + normNoise * _VoronoiMix;
                float3 vor = voronoi3(_Scale * norm, true) / _Scale;
                
                half nl = max(0, dot(vor, _MainLightPosition.xyz));
                float3 light = nl * _MainLightColor;
                float3 ambient = SampleSH(i.normal);
                col.xyz *= light + ambient;
                return col;
            }
            ENDHLSL
        }
    }
}
