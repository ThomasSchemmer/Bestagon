// Upgrade NOTE: replaced '_Object2World' with 'unity_ObjectToWorld'

Shader "Custom/HexagonShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Type ("Type", Float) = 0
        _Selected ("Selected", Float) = 0
        _Hovered("Hovered", Float) = 0
        _Adjacent("Adjacent", Float) = 0
        _Malaised("Malaised", Float) = 0
            
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
            sampler2D _MainTex;
            float4 _MainTex_ST;

            // each hexagon mesh sets these values for itself, todo: should be bitmask
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Type)
                UNITY_DEFINE_INSTANCED_PROP(float, _Selected)
                UNITY_DEFINE_INSTANCED_PROP(float, _Hovered)
                UNITY_DEFINE_INSTANCED_PROP(float, _Adjacent)
                UNITY_DEFINE_INSTANCED_PROP(float, _Malaised)
            UNITY_INSTANCING_BUFFER_END(Props)

            bool IsWater(){
                // ocean value can be checked in hexagonconfig, currently 4
                return _Type == 4;
            }

            float4 NormalLighting(float3 normal){
                // while non-water tiles use directional lighting from "sun light",
                // water tiles have to lerp between the prescribed color values in the tiles.png
                // to pass this to the fragment shader simply write it in diff.x and add to uv.x
                half nl = max(0, dot(normal, _WorldSpaceLightPos0.xyz));
                if (IsWater()){
                    return float4(1 / 16.0, 0, 0, 0);
                }else{
                    return nl * _LightColor0;
                }
            }

            g2f HandleLighting(v2g IN, g2f o, float3 triangleNormal){
                //diffuse lighting calculations
                float3 normal = IsWater() ? triangleNormal : UnityObjectToWorldNormal(IN.normal);
                float4 lighting = NormalLighting(normal);
                o.diff = IsWater() ? 0 : lighting;
                // see NormalLighting()
                o.uv = IN.uv + float2(IsWater() ? lighting.x : 0, 0);
                return o;
            }

            appdata HandleWaterDisplacement(appdata v){
                uint VertexType = (uint)(v.uv.x * 16);
                bool bIsWater = IsWater() && VertexType == 1; 
                if (!bIsWater)
                    return v;
                    
                // displace ocean decoration vertices by their world location and time to create waves
                float TimeScale = 0.3;
                float WaveScale = 0.3;
                float3 worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                float2 noise = snoise(worldPos.xz + sin(_Time.y) * TimeScale) * WaveScale;
                v.vertex += float4(noise.x, 0, noise.y, 0);
                return v;
            }

            v2g vert (appdata v)
            {
                v2g o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
                
                v = HandleWaterDisplacement(v);
                // pass through in object space, since we have to use geometry shader to update the normal anyway
                o.vertex = v.vertex;
                o.normal = v.normal;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream){
                g2f o;
                
                // since geometry shader has access to each triangle
                // we can calculate the ocean lighting here
                float4 AB = normalize(IN[1].vertex - IN[0].vertex);
                float4 AC = normalize(IN[2].vertex - IN[0].vertex);
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
                bool isBorder = i.uv.x < 1 / 16.0;
                int highlight = _Malaised > 0 ? 4 : 
                                _Selected > 0 ? 1 :
                                _Hovered > 0 ? 2 :
                                _Adjacent > 0 ? 3 : 0;
                bool isHighlighted = isBorder && highlight > 0;
                float v = isHighlighted ? 0 : _Type;
                float u = isHighlighted ? highlight - 1 : i.uv.x * 16.0;
                float2 uv = float2(u, v) / 16.0;
                fixed4 color = tex2D(_MainTex, uv);
                color *= i.diff;
                return color;
            }
            ENDCG
        }
    }
}
