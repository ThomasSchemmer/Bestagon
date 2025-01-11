/**
* Shader to render the different usability masks efficiently
* Creates SDF rounded squares for each of the categories, sorted by type (see indexing map)
*/
Shader "Custom/UsableOnShader"
{
    Properties
    {
        _TypeTex ("Types", 2D) = "white" {}
        _BoxPos ("Box Pos", Vector) = (0, 0, 0, 0)
        _BoxSize ("Box Size", Vector) = (0, 0, 0, 0)
        _TypeMask ("Type Mask", Float) = 0
    }
    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
    CBUFFER_START(UnityPerMaterial)

        float4 _TypeTex_ST;
        float4 _TypeTex_TexelSize;

        // x,y are pos 0..1
        float4 _BoxPos;
        // x,y are width/height 0..1, z and w are amount in x and y axis
        float4 _BoxSize;

        float _TypeMask;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x

            #include "Assets/Materials/Shaders/Util/Util.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };
            
            SAMPLER(_TypeTex);

            float box(float2 position, float2 halfSize, float cornerRadius) {
               position = abs(position) - halfSize + cornerRadius;
               return length(max(position, 0)) + min(max(position.x, position.y), 0) - cornerRadius;
            }

            v2f vert (appdata v)
            {
                v2f o;
                
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = VertexInputs.positionCS;
                o.uv = TRANSFORM_TEX(v.uv, _TypeTex);
                return o;
            }

            int GetHexagonType(int GroupIndex){
                // this ugly lookup is from the arangement in @HexagonConfig.SpecialTypes etc
                // should have given it a better order, but now its too late to fix everywhere
                // -1 == no type assigned to group
                
                static int TypeMap[20] = {
                    2, 3, 15, -1,   // special
                    0, 1, 10, 12,   // Meadow
                    4, 11, 16, -1,  // Desert
                    13, 7, 9, 8,    // Swamp
                    6, 14, 5, -1    // Ice
                };
                return TypeMap[GroupIndex];
            }
            
            int GetZigZagIndex(float2 uv, int maxX, int maxY){
            
                /*
                * Zigzag pattern, to keep the different categories close to each other
                * 0 1 | 4 5 | 8  9  | ..
                * 2 3 | 6 7 | 10 11 | ..
                */
                int x = round(uv.x * maxX - 0.5);
                int y = round(1 - uv.y * maxY + 0.5);
                int index = y * 2 + (int)(x / 2.0) * 4 + x % 2u;
                return index;
            }

            float4 GetTypeColor(int Type){
                if (Type == -1)
                    return 0;
                    
                // skip the first colorset, as its hover etc
                Type += 1;
                // as we have a split uv map we need to wrap around
                float u = Type >= 16 ? 0.505 : 0.005;
                float v = (Type % 16u) / 16.0;
                float2 uv = float2(u, v);
                float4 color = tex2D(_TypeTex, uv);
                return color;
            }

            bool IsInSquare(float2 pos){
                // see https://iquilezles.org/articles/sdfrepetition/
                float2 amount = 1.0 / _BoxSize.zw;
                float2 halfAmount = amount / 2.0;
                
                // create grid from 0..1
                pos += halfAmount;
                float u = pos.x - amount.x * round(pos.x / amount.x);
                float v = pos.y - amount.y * round(pos.y / amount.y);
                // bring from -x..x to 0..1
                u = (u * (1 / halfAmount.x) + 1) / 2;
                v = (v * (1 / halfAmount.y) + 1) / 2;
                // bring box to center
                float2 uv = float2(u, v) - 0.5;
                float Dis = box(uv, _BoxSize.xy, 0.1);
                return Dis < 0;
            }

            bool IsTypeInMask(int type){
                if (_TypeMask < 0)
                    return false;

                return ((int)_TypeMask & (1 << type)) > 0;
            }

            float4 frag (v2f i) : SV_Target
            {
                int index = GetZigZagIndex(i.uv, 10, 2);
                int type = GetHexagonType(index);
                if (type == -1)
                    return 0;

                float4 typeColor = GetTypeColor(type);
                float4 invisColor = 0;
                bool bIsInSquare = IsInSquare(i.uv);
                bool bIsInMask = IsTypeInMask(type);

                return bIsInSquare && bIsInMask ? typeColor : invisColor;
            }
            ENDHLSL
        }
    }
}
