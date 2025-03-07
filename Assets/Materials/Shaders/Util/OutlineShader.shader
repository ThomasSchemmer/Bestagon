/** 
* Shader to generate jumpflood-filled outlines for any unit etc 
* that is behind something else. Uses the @OutlineRenderFeature
* First pass is to overwrite basic fill of camera 
* Second pass is to stencil the mesh
* third pass is to actually draw the outline
*/

Shader "Custom/OutlineShader"
{
    Properties
    {
        _HighlightTex("Highlight Texture", 2D) = "white" {}
    }

    
    HLSLINCLUDE
        #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
        sampler2D _CameraDepthTexture;
        float4 _BrushNormalTex_ST;
        float4 _BrushNormalTex_TexelSize;
    ENDHLSL

    SubShader
    {
        //Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry+2" "LightMode" = "UniversalForward"}
        
        Tags { "RenderType"="Transparent" "Queue" = "Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
        LOD 100

        Pass {

            ZWrite Off // don't render to depth
            ZTest Off // ignore ZTest, aka always render
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert (float4 vertex : POSITION) : SV_POSITION { 
          
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(vertex.xyz);
                return VertexInputs.positionCS;
            }
            half4 frag() : SV_Target 
            { 
                // overwrite base camera color to say "empty" with z = 0
                return float4(0, 0, 0, 1); 
            }
            ENDHLSL
        }

        Pass {
            Stencil {
                Ref 2
                Comp NotEqual
                Pass Replace
            }

            ColorMask 0 //
            ZWrite Off // don't render to depth
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            float4 vert (float4 vertex : POSITION) : SV_POSITION { 
          
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(vertex.xyz);
                return VertexInputs.positionCS;
            }
            half4 frag() : SV_Target 
            { 
                return 1; 
            }
            ENDHLSL
        }

        Pass
        {
            Stencil
            {
                Ref 2
                Comp NotEqual
            }
            ZTest Greater 
            //ZWrite Off 

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            SAMPLER(_HighlightTex);

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;

            v2f vert (appdata v)
            {
                v2f o;
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(v.vertex.xyz);
                o.vertex = VertexInputs.positionCS;

                // range -ortho.x .. +ortho.x
                float2 ScreenUV = VertexInputs.positionVS.xy;
                ScreenUV.x = ScreenUV.x / unity_OrthoParams.x;
                ScreenUV.y = ScreenUV.y / unity_OrthoParams.y;
                ScreenUV = (ScreenUV + 1) / 2.0;
                // range 0..1
                o.uv = ScreenUV;
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                //return float4(0, 0, 1, 1);
                float4 ColorA = tex2D(_HighlightTex, i.uv);
                return float4(ColorA.r, 0, 0, ColorA.r);
            }
            ENDHLSL
        }
    }
}
