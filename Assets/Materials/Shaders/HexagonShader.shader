Shader"Custom/HexagonShader"
{
    Properties
    {
        _TypeTex ("Types", 2D) = "white" {}
        _NoiseTex("Noise", 2D) = "white" {}
        _Type("Type", Float) = 0
        _Selected("Selected", Float) = 0
        _Hovered("Hovered", Float) = 0
        _Adjacent("Adjacent", Float) = 0
        _Malaised("Malaised", Float) = 0
        _WorldSize("World Size", Vector) = (0, 0, 0, 0)
        _Desaturation("Desaturation", Float) = 0
        // unused debug value
        _Value("Value", Float) = 0
            
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile_instancing

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0
            #include "Assets/Materials/Shaders/Util/Util.cginc" //for snoise

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; //for diff color calcs, except water
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2g
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct g2f{
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 diff : COLOR0; //diffuse lighting for shadows
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // shared between all hexagons, simply a lookup img for colors
            sampler2D _TypeTex;
            float4 _TypeTex_ST;

            // noise texture for easier lookup / shifting water
            sampler2D _NoiseTex;
            float4 _NoiseTex_ST;

            // contains size of world in (x, y, 0, 0)
            float4 _WorldSize;

            float _Desaturation;

            // each hexagon mesh sets these values for itself, todo: should be bitmask
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Type)
                UNITY_DEFINE_INSTANCED_PROP(float, _Selected)
                UNITY_DEFINE_INSTANCED_PROP(float, _Hovered)
                UNITY_DEFINE_INSTANCED_PROP(float, _Adjacent)
                UNITY_DEFINE_INSTANCED_PROP(float, _Malaised)
                UNITY_DEFINE_INSTANCED_PROP(float, _Value)
            UNITY_INSTANCING_BUFFER_END(Props)

            inline bool IsWater(){
                // ocean value can be checked in hexagonconfig, currently 4
                return _Type == 4 || _Type == 16;
            }

            inline bool IsBorder(float2 uv) {
                return uv.x < 1.01 / 16.0;
            }

            float4 NormalLighting(float3 normal){
                // while non-water tiles use directional lighting from "sun light",
                // water tiles have to lerp between the prescribed color values in the tiles.png
                // to pass this to the fragment shader simply write it in diff.x and add to uv.x

                // range 0..1
                half nl = max(0, dot(normal, _WorldSpaceLightPos0.xyz));

                if (IsWater()){
                    // needs to be 0..3/16
                    nl = nl * 3 / 16.0;
                    return float4(nl, 0, 0, 0);
                }else{
                    return nl * _LightColor0;
                }
            }

            g2f HandleLighting(v2g IN, g2f o, float3 triangleNormal){
                //diffuse lighting calculations
                float3 normal = IsWater() ? triangleNormal : UnityObjectToWorldNormal(IN.normal);
                float4 lighting = NormalLighting(normal);
                o.diff = IsWater() ? 1 : lighting;
                bool shouldChangeUV = IsWater() && !IsBorder(IN.uv);
                o.uv = IN.uv + float2(shouldChangeUV ? lighting.x : 0, 0);
                // if we change the color too much, we suddenly declare this pixel as border!
                o.uv.x = !IsBorder(IN.uv) ? max(o.uv.x, 1.1 / 16.0) : o.uv.x;
                return o;
            }

            float4 HandleWaterDisplacement(appdata v){
                if (!IsWater() || IsBorder(v.uv))
                    return v.vertex;
                    
                // displace ocean decoration vertices by their world location and time to create waves
                // only do this for the lighting calculation - don't actually displace the vertex!
                float TimeScale = 0.01;
                float WaveWidth = 20;
                float WaveHeight = 15;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float4 worldFraction = float4(worldPos.xz / _WorldSize.xy, 0, 0);
                float4 noiseUV = float4(frac(worldFraction.xy * WaveWidth + _Time.y * TimeScale), 0, 0);
                float4 noise = tex2Dlod(_NoiseTex, noiseUV);
                return v.vertex + float4(0, noise.g * WaveHeight, 0, 0);
            }

            v2g vert (appdata v)
            {
                v2g o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                // pass through in object space, since we have to use geometry shader to update the normal anyway
                o.vertex = v.vertex;
                o.normal = v.normal;
                o.uv = TRANSFORM_TEX(v.uv, _TypeTex);

                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream){
                g2f o;
                
                // since geometry shader has access to each triangle
                // we can calculate the ocean lighting here
                float4 AB = normalize(HandleWaterDisplacement(IN[1]) - HandleWaterDisplacement(IN[0]));
                float4 AC = normalize(HandleWaterDisplacement(IN[2]) - HandleWaterDisplacement(IN[0]));
                float3 triangleNormal = cross(AB, AC);

                [unroll]
                for (int i = 0; i < 3; i++){
                    o.vertex = UnityObjectToClipPos(IN[i].vertex);
                    UNITY_TRANSFER_INSTANCE_ID(IN[i], o);
                    o = HandleLighting(IN[i], o, triangleNormal);
                    triStream.Append(o);
                }

                triStream.RestartStrip();
            }

            fixed4 frag(g2f i) : SV_Target
            {
                // each uv stores information about hex type (uv.y) and model vertex type (uv.x) 
                // the color of each vertex is dependent on its own type (border, decoration, highlight,..)
                // as well as the type of hex itself (eg desert vs ocean)
                bool isBorder = IsBorder(i.uv);
                int highlight = _Malaised > 0 ? 4 : 
                                _Selected > 0 ? 1 :
                                _Hovered > 0 ? 2 :
                                _Adjacent > 0 ? 3 : 0;
                bool isHighlighted = isBorder && highlight > 0;
    
                // as we have a split uv map we need to wrap around
                int xType = _Type / 16.0;
                int yType = _Type % 16;
                float StandardColor = (i.uv.x * 16.0) + xType * 16/2;
                float HighlightColor = highlight - 1;
                float u = isHighlighted ? HighlightColor : StandardColor;
                float v = isHighlighted ? 0 : yType;
                float2 uv = float2(u, v) / 16.0;
                float4 color = tex2D(_TypeTex, uv);
    
                // light influence
                color *= i.diff;
    
                float Desaturated = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                color.rgb = float3(
                    lerp(color.r, Desaturated, _Desaturation),
                    lerp(color.g, Desaturated, _Desaturation),
                    lerp(color.b, Desaturated, _Desaturation)
                );
                color.a = 1;
                return color;
            }
            ENDCG
        }
    }
}
