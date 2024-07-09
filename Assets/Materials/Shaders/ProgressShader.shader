Shader "Custom/ProgressShader"
{
    Properties
    {
        _TypeTex ("Type", 2D) = "white" {}
        _MinSize ("Minimum Size", Float) = 0.1
        _MaxSize ("Maximum Size", Float) = 1
        _Division("Division", Range(0, 0.1)) = 0.05
        _CurrentProgress ("Current Progress", Float) = 0
        _MaxProgress ("Max Progress", Float) = 5
        [toggle]_IsPositive("Is Positive", Float) = 0
        _PositiveColor ("Positive Color", Color) = (0, 0, 0, 1)
        _NegativeColor ("Negative Color", Color) = (0, 0, 0, 1)
    }

    
    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
    CBUFFER_START(UnityPerMaterial)
    
        float4 _TypeTex_ST;
        float4 _TypeTex_TexelSize;

        float _MinSize;
        float _MaxSize;
        float _CurrentProgress;
        float _MaxProgress;
        float _IsPositive;
        float _Division;
        float4 _PositiveColor, _NegativeColor;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        LOD 100
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #define PI 3.14159265359
            // rotate by 90°
            #define ROTATION 1.570796326795
            
            #pragma multi_compile_instancing

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 vertexOS : NORMAL;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            SAMPLER(_TypeTex);
            
            UNITY_INSTANCING_BUFFER_START(Props)
                //UNITY_DEFINE_INSTANCED_PROP(float, _MaxProgress)
                //UNITY_DEFINE_INSTANCED_PROP(float, _CurrentProgress)
                //UNITY_DEFINE_INSTANCED_PROP(float, _IsNegative)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(v.vertex.xyz);

                v2f o;
                
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = VertexInputs.positionCS;
                o.vertexOS = v.vertex;
                o.uv = TRANSFORM_TEX(v.uv, _TypeTex);
                return o;
            }

            float getAngle(float2 uv){
                // rotate 90° and scale to 0..1
                float angle = atan2(uv.y, uv.x) - ROTATION;
                angle += angle < 0 ? 2 * PI : 0;
                angle /= 2 * PI;
                angle = 1 - angle;
                return angle;
            }

            half4 frag (v2f i) : SV_Target
            {
                float4 color = 0;
                
                float2 uv = (i.uv - 0.5) * 2;
                float size = length(uv);

                bool bIsInRange = size < _MaxSize && size > _MinSize;
                if (bIsInRange)
                {
                    float angle = getAngle(uv);
                    float progress = angle * _MaxProgress;
                    int uprogress = progress;
                    float fprogress = frac(progress);

                    bool bIsInCount = uprogress <= _CurrentProgress - 1;
                    bool bIsntDivision = fprogress > _Division && fprogress < 1 - _Division;

                    bool bIsColored = bIsInRange && bIsInCount && bIsntDivision;
                    color = _IsPositive ? _PositiveColor : _NegativeColor;
                    color = bIsColored ? color : 0;

                }
                else
                {
                    // rescale and recenter
                    float2 picUV = i.uv / _MinSize;
                    picUV += 0.5 - 1 / _MinSize / 2;
                    color = tex2D(_TypeTex, picUV);
                }

                return color;
            }
            ENDHLSL
        }
        
    }
}
