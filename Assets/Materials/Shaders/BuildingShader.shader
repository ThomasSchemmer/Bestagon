Shader "Custom/BuildingShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }

    
    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
    CBUFFER_START(UnityPerMaterial)

        float4 _MainTex_ST;
        float4 _MainTex_TexelSize;
    CBUFFER_END
    ENDHLSL
    SubShader
    {

        Tags { "RenderType"="Opaque" "Queue" = "Geometry-1" "RenderPipeline" = "UniversalPipeline" "LightMode" = "UniversalForward"}
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // shadows and ambient light

            SAMPLER(_MainTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; //for diff color calcs
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 diff : COLOR0; //diffuse lighting for shadows
                float3 vertexWS : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(v.vertex.xyz);
                VertexNormalInputs NormalInputs = GetVertexNormalInputs(v.normal);

                o.vertex = VertexInputs.positionCS;
                o.vertexWS = VertexInputs.positionWS;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                //diffuse lighting
                half nl = max(0, dot(NormalInputs.normalWS, _MainLightPosition.xyz));
                o.diff = nl *_MainLightColor;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                half4 color = tex2D(_MainTex, i.uv);
                color.xyz *= i.diff.xyz;

                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = i.vertexWS;
                float4 shadowCoord = GetShadowCoord(vertexInput);
                half shadowAttenutation = MainLightRealtimeShadow(shadowCoord);
                color = lerp(float4(0.01, 0.01, 0.03, 1), color, shadowAttenutation);

                return color;
            }
            ENDHLSL
        }
        
        // shadow casting support
        UsePass"Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
