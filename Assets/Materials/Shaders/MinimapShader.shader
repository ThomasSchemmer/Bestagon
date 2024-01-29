Shader "Custom/MinimapShader"
{
    Properties
    {
        _LineColor("Line Color", Vector) = (0.7, 0.7, 0.7, 1)
        _TypesTex ("Types", 2D) = "white" {}

        _HexDistance("HexDistance", Vector) = (0, 0, 0, 0)
        _HexPerLine("Hexes per Line", int) = 0

        _TopView("TopView", Vector) = (0, 1, 0, 0)
        _BottomView("BottomView", Vector) = (1, 1, 0, 0)
    }

     SubShader
     {
        Blend One Zero

        Pass
        {
            Name "MinimapShader"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #include "Assets/Materials/Shaders/Util/Util.cginc" 
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            struct HexagonData {
                //contains "bIsMalaised" at bit 0
                uint Type;
            };

            sampler2D _TypesTex;
            float4 _TypesTex_ST;
            float4 _LineColor;

            // in tile space
            float4 _HexDistance;
            int _HexPerLine;

            // in uv space, each contains two uv vectors
            float4 _TopView;
            float4 _BottomView;

            StructuredBuffer<HexagonData> HexagonBuffer;

            float4 GetColor(float2 uv){
                // map 0..1 uv to global tile location
                float x = (int)(uv.x * _HexDistance.x);
                float y = (int)(uv.y * _HexDistance.y);

                // map hex location to index
                int Index = x + y * _HexPerLine;

                // convert data to actual colour
                HexagonData Data = HexagonBuffer[Index];
                uint Malaised = Data.Type & 0x80;
                uint Type = Data.Type & 0x7F;
    
                float U = Type * 0.5 + 0.5;
                float V = Type % 16;
                float2 MappedUV = float2(U, V) / 16.0;

                float4 Colour = tex2D(_TypesTex, MappedUV);
                float4 MalaisedColor = tex2D(_TypesTex, float2(3 / 16.0, 0));
                return Malaised > 0 ? MalaisedColor : Colour;
            }

            // https://gist.github.com/unitycoder/2fe0bfd2498041dedd5c326c9d4c727e
            float4 distanceLine(float2 uv, float2 a, float2 b){
                float2 pa = uv - a;
                float2 ba = b - a;
                float h = saturate(dot(pa, ba) / dot(ba, ba));
                float2 d = pa - ba * h;
                return dot(d,d);
            }

            /**
             * returns the minimum distance to any of the lines representing the view frustum
             *  A---------------------------B
             *   \                         /
             *    \                       /
             *    D-----------------------C
             * These values get set from the camera, by setting them into a v4(AB) and v4(DC)
             */
            float distanceLines(float2 uv){
                float DistanceToAB = distanceLine(uv, _TopView.xy, _TopView.zw);
                float DistanceToBC = distanceLine(uv, _TopView.zw, _BottomView.zw);
                float DistanceToCD = distanceLine(uv, _BottomView.zw, _BottomView.xy);
                float DistanceToDA = distanceLine(uv, _BottomView.xy, _TopView.xy);
                return min(DistanceToAB, min(DistanceToBC, min(DistanceToCD, DistanceToDA)));
            }

            float4 frag(v2f_customrendertexture IN) : COLOR
            {
                float4 Color = GetColor(IN.localTexcoord.xy);
                float LineDistance = distanceLines(IN.localTexcoord.xy);
                return lerp(_LineColor, Color, smoothstep(0.0, 0.0001, LineDistance));
            }
            ENDCG
        }
    }
}
