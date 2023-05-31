Shader "Custom/MalaiseShader"
{
    Properties
    {
        _Direction ("Direction", Vector) = (0, 0, 0)
        _Scale ("Scale", Vector) = (0, 0, 0)
        _V0Scale ("V0 Scale", Float) = 0
        _V1Scale ("V1 Scale", Float) = 0
        _Blend("V0 V1 Blen", Range(0.0, 1.0)) = 0.5
        _AlphaRamp ("Alpha Ramp", Vector) = (0, 0, 0)
        _Color("Color", Color) = (1, 1, 1, 1)
    }
    SubShader
    {
        Tags {"Queue"="Transparent" "RenderType"="Transparent"}
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Materials/Shaders/Util/Util.cginc"

            float _V0Scale;
            float _V1Scale;
            float _Blend;
            float3 _Scale;
            float3 _AlphaRamp;
            float3 _Direction;
            float4 _Color;

            float voronoi(float2 position){                            
                float2 base = floor(position);
                float minDistanceSqr = 10000;
                [unroll]
                for (int x = -1; x <= 1; x++){
                    [unroll]   
                    for (int y = -1; y <= 1; y++){
                        float2 cell = base + float2(x, y);
                        float2 posInCell = cell + hash22(cell);
                        float2 diff = posInCell - position;
                        float distanceSqr = diff.x * diff.x + diff.y * diff.y;
                        if (distanceSqr < minDistanceSqr){
                            minDistanceSqr = distanceSqr;
                        }
                    }
                }
                return minDistanceSqr;
            }

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 worldPos : TEXCOORD1;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.uv = v.uv;
                return o;
            }
            
            // blend together two voronoi to generate cloud-like shapes
            float cloud(float2 position, float2 Direction){
                position = float2(position.x / _Scale.x, position.y / _Scale.y) + _Time.y * Direction;
                float v0 = voronoi(position / _V0Scale);
                float v1 = voronoi(position / _V1Scale);
                float v = v0 * _Blend + v1 * (1 - _Blend);
                return v;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 normal = float3(0, 1, 0);
                float c = cloud(i.worldPos.xz, _Direction);
                                
                float alpha = clamp(c, _AlphaRamp.x, _AlphaRamp.y);
                alpha = 1 - map(alpha, _AlphaRamp.x, _AlphaRamp.y, 0, 1);

                float3 color = float3(1, 1, 1);
                
                // alpha represents how many border tiles are also malaised (uv.x = 1 if both are infected)
                return float4(color, alpha * i.uv.x);
            }
            ENDCG
        }
    }
}
