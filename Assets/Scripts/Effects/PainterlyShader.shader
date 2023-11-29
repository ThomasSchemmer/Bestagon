// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Unlit/Painterly"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        ZWrite Off
        Blend SrcAlpha
        OneMinusSrcAlpha
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct MeshProperties {
                float3 position;
                float4 quat;
                float4 color;
            };

            StructuredBuffer<MeshProperties> _Properties;

            sampler2D _MainTex;
            float4 _MainTex_ST;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
            };


            // --------- QUATERNIONS
            // https://gist.github.com/mattatz/40a91588d5fb38240403f198a938a593
            float4 qmul(float4 q1, float4 q2)
            {
                return float4(
                    q2.xyz * q1.w + q1.xyz * q2.w + cross(q1.xyz, q2.xyz),
                    q1.w * q2.w - dot(q1.xyz, q2.xyz)
                );
            }

            float3 rotate_vector(float3 v, float4 r)
            {
                float4 r_c = r * float4(-1, -1, -1, 1);
                return qmul(r, qmul(float4(v, 0), r_c)).xyz;
            }
            // --------- END OF QUATERNIONS
            


            v2f vert (appdata v, uint instanceID: SV_InstanceID)
            {
                MeshProperties Props = _Properties[instanceID];
                v2f o;
                v.vertex.x *= 4.58f;
                // rotate to actual triangle position
                v.vertex.xyz = rotate_vector(v.vertex.xyz, Props.quat);
                // move to centre of triangle
                v.vertex += float4(Props.position, 1);
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = Props.color;
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float4 color = tex2D(_MainTex, i.uv);
                color.xyz *= i.color.xyz;
                return color;
            }
            ENDCG
        }
    }
}
