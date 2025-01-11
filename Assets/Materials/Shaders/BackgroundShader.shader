Shader"Custom/BackgroundShader"
{
    Properties
    {
        [Header(Folds)][Space]
        _ColorA("Color A", Color) = (1, 1, 1, 1)
        _ColorB("Color B", Color) = (1, 1, 1, 1)
        _NoiseScale("Noise Scale", float) = 1
        _NoiseIterations("Noise Iterations", float) = 1
        _NoiseFactor("Noise Factor", float) = 1
        _FoldScale("Scale", float) = 0.001
        _FoldIntensity("Intensity", float) = 1
        _LowerDistance("Min Distance", float) = 1
        _UpperDistance("Max Distance", float) = 4

        [Header(Flats)][Space]
        _FlatIntensity("Intensity", float) = 1

        [Header(Border)][Space]
        _BorderThickness("BorderThickness", float) = 1
        _BorderOffset("BorderOffset", float) = 3
        _BorderColor("BorderColor", Color) = (1, 1, 1, 1)

        [Header(Util)][Space]
        _WorldMin("WorldMin", Vector) = (0,0,0,0)
        _WorldMax("WorldMax", Vector) = (300, 300, 0, 0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "Assets/Materials/Shaders/Util/Util.cginc" //for snoise

            struct appdata
            {
                float4 vertex : POSITION;
                float4 world : NORMAL;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 world : NORMAL;
                float4 vertex : SV_POSITION;
            };

            float4 _ColorA;
            float4 _ColorB;
            float _NoiseScale;
            float _NoiseIterations;
            float _NoiseFactor;
            float _FoldScale;
            float _FoldThickness;
            float _FoldIntensity;
            float _FlatIntensity;
            float _LowerDistance;
            float _UpperDistance;
            float _BorderOffset;
            float4 _WorldMin;
            float4 _WorldMax;
            float _BorderThickness;
            float4 _BorderColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.world = mul(unity_ObjectToWorld, v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float Alpha = ssnoise(float3(i.world.xz / 100.0, 0), _NoiseScale, 0, _NoiseIterations, _NoiseFactor);
                Alpha = clamp(Alpha, 0, 1);
                float4 Color = lerp(_ColorA, _ColorB, Alpha);
    
                int CellID;
                float DistanceDiff;
                LineVoronoi(i.world.xz * _FoldScale, DistanceDiff, CellID);
                float FoldModifier = smoothstep(_LowerDistance, _UpperDistance, DistanceDiff) * _FoldIntensity;

                float FlatModifier = CellID / 10.0 * _FlatIntensity;
                Color = Color * (1 - FoldModifier) * (1 - FlatModifier);

                float2 WorldPos = i.world.xz + float2(5, 6);
                float2 BorderMin = 0;
                float2 BorderMax = _WorldMax;
                float2 BorderDistanceMin = float2(pow(WorldPos.x - BorderMin.x, 2), pow(WorldPos.y - BorderMin.y, 2));
                float2 BorderDistanceMax = float2(pow(WorldPos.x - BorderMax.x, 2), pow(WorldPos.y - BorderMax.y, 2));
                float2 BorderValueMin = float2(
                    smoothstep(_BorderThickness, -_BorderThickness, sqrt(BorderDistanceMin.x)),
                    smoothstep(_BorderThickness, -_BorderThickness, sqrt(BorderDistanceMin.y))
                );
                float2 BorderValueMax = float2(
                    smoothstep(_BorderThickness, -_BorderThickness, sqrt(BorderDistanceMax.x)),
                    smoothstep(_BorderThickness, -_BorderThickness, sqrt(BorderDistanceMax.y))
                );
                float BorderValue = BorderValueMin.x + BorderValueMin.y + BorderValueMax.x + BorderValueMax.y;
                //border should not be drawn into infinity
                float IsInMap = 
                    (WorldPos.x + _BorderOffset >= _WorldMin.x && WorldPos.y + _BorderOffset >= _WorldMin.y  &&
                    WorldPos.x - _BorderOffset < _WorldMax.x && WorldPos.y - _BorderOffset < _WorldMax.y) ? 1 : 0;
                BorderValue *= IsInMap;

                Color = BorderValue * _BorderColor + (1 - BorderValue) * Color;
                return Color;
}
            ENDCG
        }

        // depth passes
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }
}
