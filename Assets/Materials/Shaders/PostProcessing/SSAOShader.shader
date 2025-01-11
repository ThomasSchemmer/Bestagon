/** 
 */
Shader "Custom/PostProcessing/SSAO"
{
    Properties
    {

    }


    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    
    struct Attributes {
        uint vertexID : SV_VertexID;
    };


    CBUFFER_START(UnityPerMaterial)
        sampler2D _CameraNormalsTexture;
        
        float4 _Kernel[20];
        float _KernelSize;
    CBUFFER_END

    
    //TEXTURE2D_X(_CameraColorTexture);
    //SAMPLER(sampler_CameraColorTexture);
    //TEXTURE2D_X(_CameraDepthTexture);  _CameraNormalsTexture
    //SAMPLER(sampler_CameraDepthTexture);
    
sampler2D _CameraDepthTexture;

    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" "RenderPipeline" = "UniversalPipeline" }
        
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Cull Off

        Pass
        {
            Name "SSAO"
            HLSLPROGRAM
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            //#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareNormalsTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            
            #pragma multi_compile __ ENABLE_LIGHT_PASS

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0

            #pragma enable_d3d11_debug_symbols //renderdoc
            
            struct Varyings {
	            float4 positionCS 	: SV_POSITION;
                float3 positionWS : TEXCOORD1;
	            float2 uv		: TEXCOORD0;
            };
            

            Varyings vert(Attributes i) {
				Varyings OUT;
                
                /** create a fullscreen rect out of thin air */
                float4 pos = GetFullScreenTriangleVertexPosition(i.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(i.vertexID);
				OUT.positionCS = pos;
                // ignore cause we will calculate by UV (prolly improvable)
                OUT.positionWS = 0;
				OUT.uv = uv;
				return OUT;
			}

            float4 frag(Varyings input) : SV_Target
            {
            
                // query info about the camera and screen
                float2 size = unity_OrthoParams.xy;
                float3 forwardDir = mul((float3x3)unity_CameraToWorld, float3(0,0,1));
                float3 upDir = mul((float3x3)unity_CameraToWorld, float3(0,1,0));
                float3 rightDir = mul((float3x3)unity_CameraToWorld, float3(1,0,0));
                float depth = 1 - tex2D(_CameraDepthTexture, input.uv).r;
                float planeDepth = _ProjectionParams.z - _ProjectionParams.y;

                if (depth > 0.9999)
                    return 0;

                // get world position of each pixel, starting from the cam center
                float2 center = (input.uv - 0.5) * 2;
                float3 pos = _WorldSpaceCameraPos;
                pos += center.x * rightDir * size.x;
                pos += center.y * upDir * size.y;

                // move along view dir by depth to get pixel world position
                // luckily this is orthografic, so we can use the forward view vector
                pos += forwardDir * _ProjectionParams.y;
                // scale with depth to camera end plane, resulting in world space pos
                pos += forwardDir * (_ProjectionParams.z - _ProjectionParams.y) * depth; 
                                
                //TODO: apply random sampling in cone

                // convert world space pos back to uv coords to check the depth
                float4 off = float4(pos, 1);
                // results in -screensize..+screeensize, not in 0..1
                off = mul(unity_WorldToCamera, off);

                off.xy = float2(off.x / size.x, off.y / size.y);
                off.xy = (off.xy + 1) / 2;
                return float4(off.xy, 0, 1);
                //return float4(_Kernel[5].xyz, 1);

                //return float4(tex2D(_CameraNormalsTexture, input.uv).r, 0, 0, 0.5);
                //float4 Color = float4(D, D, D, 1);
                //return Color * Color;

                /*  
                
   offset = uProjectionMat * offset;
   offset.xy /= offset.w;
   offset.xy = offset.xy * 0.5 + 0.5;

                */
            }
            ENDHLSL
        } // end Pass


    } // end SubShader

    
}
