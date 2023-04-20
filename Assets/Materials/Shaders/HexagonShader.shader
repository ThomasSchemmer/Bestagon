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
            #pragma fragment frag

            #include "UnityCG.cginc"
            #include "UnityLightingCommon.cginc" // for _LightColor0

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL; //for diff color calcs
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 diff : COLOR0; //diffuse lighting for shadows
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Type)
                UNITY_DEFINE_INSTANCED_PROP(float, _Selected)
                UNITY_DEFINE_INSTANCED_PROP(float, _Hovered)
                UNITY_DEFINE_INSTANCED_PROP(float, _Adjacent)
                UNITY_DEFINE_INSTANCED_PROP(float, _Malaised)
            UNITY_INSTANCING_BUFFER_END(Props)

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);

                //diffuse lighting
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl *_LightColor0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
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
