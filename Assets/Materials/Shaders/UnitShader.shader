
Shader"Custom/UnitShader"
{
    Properties
    {
        _UnitColor ("Unit Color", Color) = (0, 0, 0, 1)

        [Header(Painterly)][Space]
        _NormalNoiseScale("Normal Noise Scale", Range(10, 40)) = 20
        _NormalNoiseEffect("Normal Noise Effect", Range(0, 0.3)) = 0.1
        _VoronoiScale ("Voronoi Scale", Range(0, 10)) = 6
        _EdgeBlendFactor("Edge Blend Factor", Range(0, 0.3)) = 0.1
        _CenterBlendFactor("Center Blend Factor", Range(0, 0.3)) = 0.1
        _BrushNormalTex("Brush Texture", 2D) = "white" {}
    }

    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
    CBUFFER_START(UnityPerMaterial)

        float4 _UnitColor;

            //painterly
        float _VoronoiScale;
        float _NormalNoiseScale;
        float _NormalNoiseEffect;
        float _EdgeBlendFactor;
        float _CenterBlendFactor;
        float4 _BrushNormalTex_ST;
        float4 _BrushNormalTex_TexelSize;

    CBUFFER_END
    ENDHLSL

    SubShader
    {
        LOD 100
        
        
        /************************ BEGIN UNIT SHADER *************************/

        Pass 
        {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry+0" "LightMode" = "UniversalForward"}
        
            HLSLPROGRAM

            #pragma require geometry
            
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
 
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"  
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // shadows and ambient light

            #include "Assets/Materials/Shaders/Util/Util.cginc" //for snoise
            #include "Assets/Materials/Shaders/Util/Painterly.cginc" 

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; //for diff color calcs
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float3 vertexWS : TEXCOORD1;
                float3 vertexOS : TEXCOORD2;
            };

            SAMPLER(_BrushNormalTex);

            float4 DiffuseLighting(float3 normal){
                half nl = max(0, dot(normal, _MainLightPosition.xyz));
                return nl * _MainLightColor;
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                VertexNormalInputs NormalInputs = GetVertexNormalInputs(v.normal);
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = VertexInputs.positionCS;
                o.vertexOS = v.vertex.xyz;
                o.vertexWS = VertexInputs.positionWS;
                o.normal = NormalInputs.normalWS;
                            
                return o;
            }

            painterlyInfo getPainterlyInfo(v2f i){
                painterlyInfo info;
                info.normal = i.normal;
                info.vertexWS = i.vertexWS;
                info.vertexOS = i.vertexOS;
                info._NormalNoiseScale = _NormalNoiseScale;
                info._NormalNoiseEffect = _NormalNoiseEffect;
                info._VoronoiScale = _VoronoiScale;
                info._EdgeBlendFactor = _EdgeBlendFactor;
                info._CenterBlendFactor = _CenterBlendFactor;
                return info;
            }

            half4 frag(v2f i) : SV_Target
            {
                float4 baseColor = _UnitColor;
                float3 painterlyNormal = painterly(getPainterlyInfo(i), _BrushNormalTex);
                float4 painterlyLight = max(0, dot(painterlyNormal, _MainLightPosition.xyz)) * _MainLightColor;
        
                // light influence (both normal-based and ambient)
                float4 diffuseLight = DiffuseLighting(i.normal);
                float4 ambientLight = float4(SampleSH(i.normal), 1);
                float4 globalLight = diffuseLight + ambientLight * 0.6; 

                float4 color = baseColor * painterlyLight * globalLight;
    
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

        /************************ END UNIT SHADER *************************/

        
    }
}
