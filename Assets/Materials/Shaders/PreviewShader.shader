Shader "Custom/PreviewShader"
{
    Properties
    {
        _Allowed ("Allowed", Float) = 0
        _YesColor("YesColor", Color) = (0, 1, 0, 0.5)
        _NoColor("NoColor", Color) = (1, 0, 0, 0.5)
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
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
                float3 normal : NORMAL; //for diff color calcs
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 diff : COLOR0; //diffuse lighting for shadows
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Allowed)
                UNITY_INSTANCING_BUFFER_END(Props)

            float4 _YesColor, _NoColor;

            v2f vert (appdata v)
            {
                v2f o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);

                o.vertex = UnityObjectToClipPos(v.vertex);

                //diffuse lighting
                half3 worldNormal = UnityObjectToWorldNormal(v.normal);
                half nl = max(0, dot(worldNormal, _WorldSpaceLightPos0.xyz));
                o.diff = nl *_LightColor0;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                fixed4 color = _Allowed ? _YesColor : _NoColor;
                color *= i.diff;
                return color;
            }
            ENDCG
        }
    }
}
