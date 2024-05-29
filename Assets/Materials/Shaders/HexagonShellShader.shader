Shader "Custom/HexagonShellShader"
{
    Properties
    {
        [Header(Shell)][Space]
        _NormalOffset ("Normal Offset", Range(0, 5)) = 0.2
        _NormalCutoff ("Normal Cutoff", Range(0, 1)) = 0.5
        
        [Header(Painterly)][Space]
        _NormalNoiseScale("Normal Noise Scale", Range(1, 40)) = 20
        _NormalNoiseEffect("Normal Noise Effect", Range(0, 0.3)) = 0.1
        _VoronoiScale ("Voronoi Scale", Range(0, 10)) = 6
        _EdgeBlendFactor("Edge Blend Factor", Range(0, 0.3)) = 0.1
        _CenterBlendFactor("Center Blend Factor", Range(0, 0.3)) = 0.1
        _BrushNormalTex("Brush Texture", 2D) = "white" {}
    }

    
    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
    CBUFFER_START(UnityPerMaterial)

        float _NormalCutoff;
        float _NormalOffset;

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
        Tags { "RenderType"="Transparent" "Queue" = "Transparent"  "RenderPipeline" = "UniversalPipeline"}
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma require geometry
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #define NUM_ITERATIONS 3
            #define NUM_TRIANGLES (3 * NUM_ITERATIONS)
            
            #include "Assets/Materials/Shaders/Util/Util.cginc" //for hash
            #include "Assets/Materials/Shaders/Util/Painterly.cginc" 

            
            SAMPLER(_BrushNormalTex);

            // We want the color information of the hexes, but cannot simply recompute them
            // as we are in a post processing shader and dont have the instancing
            TEXTURE2D(_CameraOpaqueTexture);            
            SAMPLER(sampler_CameraOpaqueTexture);

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct g2f{
                float4 vertexCS : SV_POSITION;
                float3 normal : NORMAL;
                float3 camDir : TEXCOORD1;
                float3 vertexOS : TEXCOORD2;
                float3 vertexWS : TEXCOORD3;
            };


            v2g vert (appdata v)
            {
                v2g o;
                o.vertex = v.vertex;
                o.normal = v.normal;
                o.uv = TRANSFORM_TEX(v.uv, _BrushNormalTex);
                return o;
            }

            /** Copy each triangle X times and offset it slightly to create the shells */
            [maxvertexcount(NUM_TRIANGLES)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream){
                g2f o;
                
                int TriangleCount = 0;
                [unroll]
                for (int i = 0; i < NUM_TRIANGLES; i++){
                    int TriangleIndex = i % 3;
                    int IterationIndex = i / 3;
                    float Scale = IterationIndex * (_NormalOffset / NUM_ITERATIONS) - _NormalOffset / 2;
                    
                    VertexNormalInputs NormalInputs = GetVertexNormalInputs(IN[TriangleIndex].normal);
                    float4 normalCS = mul(UNITY_MATRIX_VP, float4(NormalInputs.normalWS * Scale, 0));

                    VertexPositionInputs VertexInputs = GetVertexPositionInputs(IN[TriangleIndex].vertex.xyz);
                    o.vertexCS = VertexInputs.positionCS + normalCS;
                    o.vertexWS = VertexInputs.positionWS + NormalInputs.normalWS;
                    o.vertexOS = IN[TriangleIndex].vertex.xyz + IN[TriangleIndex].normal;

                    o.normal = NormalInputs.normalWS;
                    o.camDir = GetWorldSpaceViewDir(VertexInputs.positionWS);
                    triStream.Append(o);

                    TriangleCount ++;
                    if (TriangleCount == 3){
                        triStream.RestartStrip();
                        TriangleCount = 0;
                    }
                }

                triStream.RestartStrip();
            }

            painterlyInfo getPainterlyInfo(g2f i){
            
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

            half4 frag (g2f i) : SV_Target
            {
                float3 painterlyNormal = painterly(getPainterlyInfo(i), _BrushNormalTex);
                float painterlyAlpha = max(0, dot(painterlyNormal, i.camDir.xyz));
                if (painterlyAlpha > _NormalCutoff)
                    return 0;

                float4 screenUV = TransformWorldToHClip(i.vertexWS);
                screenUV.xy = screenUV.xy * 0.5f + 0.5f;
                screenUV.y = 1 - screenUV.y;
                float4 screenColor = SAMPLE_TEXTURE2D(_CameraOpaqueTexture, sampler_CameraOpaqueTexture, screenUV);

                return float4(screenColor.xyz, 1);
            }
            ENDHLSL
        }
        
    }
}
