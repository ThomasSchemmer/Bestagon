/** 
 * Shader to display the hexagon tiles properly. Includes:
 * - Color mapping by type (instanced)
 *      Each hexagon is assigned a hex type. The color per vertex is stored in a split 16x16 texture
 * - Simulated water movement and coloring by world position
 *      Use geometry shader to temporarily move each water triangle according to noise. Use as input for 
 *      normal lighting and store this as offset in UV ("move vertex index in NoiseMap")
 * - Border highlight according to mouseover status (instanced)
 * - Diffuse lighting
 * - Desaturation (to make tokens more visible)
 * - Shadows

 * Note: Ignore syntax errors in VisualStudio, it doesn't like shaders :(
*/

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

    
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
	
    CBUFFER_START(UnityPerMaterial)

        float4 _TypeTex_ST;
        float4 _TypeTex_TexelSize;
        float4 _NoiseTex_ST;
        float4 _NoiseTex_TexelSize;

        // contains size of world in (x, y, 0, 0)
        float4 _WorldSize;

        float _Desaturation;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline"}
        LOD 100

        Pass 
        {
            Tags {"LightMode" = "UniversalForward"}
            HLSLPROGRAM
            #pragma vertex vert
            #pragma geometry geom
            #pragma fragment frag

            #pragma multi_compile_instancing
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
 
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma target 2.0

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"  
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // shadows

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

            struct g2f
            {
                float4 vertex : SV_POSITION;
                float3 vertexWS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 diff : COLOR0; //diffuse lighting for shadows
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // shared between all hexagons, lookup img for colors
            SAMPLER(_TypeTex);
            // noise texture for easier lookup / shifting water
            SAMPLER(_NoiseTex);

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
                half nl = max(0, dot(normal, _MainLightPosition.xyz));

                if (IsWater()){
                    // needs to be 0..3/16
                    nl = nl * 3 / 16.0;
                    return float4(nl, 0, 0, 0);
                }else{
                    return nl * _MainLightColor;
                }
            }

            g2f HandleLighting(v2g IN, g2f o, float3 triangleNormal){
                //diffuse lighting calculations
                VertexNormalInputs NormalInputs = GetVertexNormalInputs(IN.normal);
                float3 normal = IsWater() ? triangleNormal : NormalInputs.normalWS;
                float4 lighting = NormalLighting(normal);
                o.diff = IsWater() ? 1 : lighting;
                bool shouldChangeUV = IsWater() && !IsBorder(IN.uv);
                o.uv = IN.uv + float2(shouldChangeUV ? lighting.x : 0, 0);
                // if we change the color too much, we suddenly declare this pixel as border!
                o.uv.x = !IsBorder(IN.uv) ? max(o.uv.x, 1.1 / 16.0) : o.uv.x;
                return o;
            }

            float3 HandleWaterDisplacement(appdata v){
                if (!IsWater() || IsBorder(v.uv))
                    return v.vertex.xyz;
                    
                // displace ocean decoration vertices by their world location and time to create waves
                // only do this for the lighting calculation - don't actually displace the vertex!
                float TimeScale = 0.01;
                float WaveWidth = 20;
                float WaveHeight = 15;
                VertexPositionInputs VertexInput = GetVertexPositionInputs(v.vertex.xyz);
                float3 worldPos = VertexInput.positionWS;
                float4 worldFraction = float4(worldPos.xz / _WorldSize.xy, 0, 0);
                float4 noiseUV = float4(frac(worldFraction.xy * WaveWidth + _Time.y * TimeScale), 0, 0);
                float4 noise = tex2Dlod(_NoiseTex, noiseUV);
                return v.vertex.xyz + float3(0, noise.g * WaveHeight, 0);
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
                float3 AB = normalize(HandleWaterDisplacement(IN[1]) - HandleWaterDisplacement(IN[0]));
                float3 AC = normalize(HandleWaterDisplacement(IN[2]) - HandleWaterDisplacement(IN[0]));
                float3 triangleNormal = cross(AB, AC);

                [unroll]
                for (int i = 0; i < 3; i++){
                    VertexPositionInputs VertexInputs = GetVertexPositionInputs(IN[i].vertex.xyz);
                    o.vertex = VertexInputs.positionCS;
                    o.vertexWS = VertexInputs.positionWS;
                    UNITY_TRANSFER_INSTANCE_ID(IN[i], o);
                    o = HandleLighting(IN[i], o, triangleNormal);
        
                    triStream.Append(o);
        
                }

                triStream.RestartStrip();
            }

            half4 frag(g2f i) : SV_Target
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
    
//#ifdef _MAIN_LIGHT_SHADOWS
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = i.vertexWS;
 
                float4 shadowCoord = GetShadowCoord(vertexInput);
                half shadowAttenutation = MainLightRealtimeShadow(shadowCoord);
                color = lerp(float4(0,0,0,1), color, shadowAttenutation);
//#endif
    
                return color;
            }
            ENDHLSL
        }

        

        // shadow casting support
UsePass"Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
