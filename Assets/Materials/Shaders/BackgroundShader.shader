Shader"Custom/BackgroundShader"
{
    Properties
    {
        _ColorA("ColorA", Color) = (1, 1, 1, 1)
        _ColorB("ColorB", Color) = (1, 1, 1, 1)
        _NoiseScale("NoiseScale", float) = 1
        _NoiseIterations("NoiseIterations", float) = 1
        _NoiseFactor("NoiseFactor", float) = 1
        _FoldScale("FoldScale", float) = 0.001
        _FoldThickness("FoldThickness", float) = 1
        _FoldIntensity("FoldIntensity", float) = 1
        _FlatIntensity("FlatIntensity", float) = 1
        _WorldMin("WorldMin", Vector) = (0,0,0,0)
        _WorldMax("WorldMax", Vector) = (300, 300, 0, 0)
        _BorderThickness("BorderThickness", float) = 1
        _BorderColor("BorderColor", Color) = (1, 1, 1, 1)
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
    
                bool bIsFold;
                int CellID;
                LineVoronoi(i.world.xz * _FoldScale, _FoldThickness, bIsFold, CellID);
                float FoldModifier = bIsFold * _FoldIntensity;
                // todo: use hashed normal per cell instead of cell id for shading
                float FlatModifier = CellID / 10.0 * _FlatIntensity;
                Color = Color * (1 - FoldModifier) * (1 - FlatModifier);
        
                // todo: make actual borders
                bool bIsBorder = abs(i.world.xy - _WorldMin.xy) < _BorderThickness || abs(i.world.xz - _WorldMax.xy) < _BorderThickness;
                //Color = bIsBorder * _BorderColor + !bIsBorder * Color; //
                return Color;
}
            ENDCG
        }
    }
}
