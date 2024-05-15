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

//painterly
        _Scale ("Scale", Float) = 1
        _NoiseScale ("NoiseScale", Range(1, 3)) = 2
        _NoiseMix("NoiseVoronoiMix", Range(0, 0.3)) = 0.1
        _BrushVoronoiOffset("BrushVoronoiOffset", Range(0, 0.3)) = 0.1
        _BrushVoronoiMix("BrushVoronoiMix", Range(0, 0.3)) = 0.1
        _Offset("CenterOffset", Vector) = (0,0,0)
        _BrushNormalTex("BrushNormal", 2D) = "white" {}
            
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

        //painterly
        float _Scale;
        float _NoiseScale;
        float _NoiseMix;
        float _BrushVoronoiOffset;
        float _BrushVoronoiMix;
        float3 _Offset;
        float4 _BrushNormalTex_ST;
        float4 _BrushNormalTex_TexelSize;
    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry+0"}
        LOD 100

        Pass 
        {
            Tags {"LightMode" = "UniversalForward"}
            HLSLPROGRAM

            #pragma require geometry
            
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

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"  
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // shadows
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // for ambient light

            #include "Assets/Materials/Shaders/Util/Util.cginc" //for snoise
            #include "Assets/Materials/Shaders/Util/BlendModes.cginc" 
            #include "Assets/Materials/Shaders/Util/Painterly.cginc" 

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
                float4 vertexCS : SV_POSITION;
                float3 vertexWS : NORMAL;
                float2 uv : TEXCOORD0;
                float4 diff : COLOR0; //diffuse lighting for shadows
                float3 normal : TEXCOORD1;
                float3 centerWorld : TEXCOORD2;
                float3 vertexOS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // shared between all hexagons, lookup img for colors
            SAMPLER(_TypeTex);
            // noise texture for easier lookup / shifting water
            SAMPLER(_NoiseTex);
            SAMPLER(_BrushNormalTex);

            // each hexagon mesh sets these values for itself, todo: should be bitmask
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Type)
                UNITY_DEFINE_INSTANCED_PROP(float, _Selected)
                UNITY_DEFINE_INSTANCED_PROP(float, _Hovered)
                UNITY_DEFINE_INSTANCED_PROP(float, _Adjacent)
                UNITY_DEFINE_INSTANCED_PROP(float, _Malaised)
                UNITY_DEFINE_INSTANCED_PROP(float, _PreMalaised)
                UNITY_DEFINE_INSTANCED_PROP(float, _Discovery)
                UNITY_DEFINE_INSTANCED_PROP(float, _Value)
            UNITY_INSTANCING_BUFFER_END(Props)

            inline bool IsWater(){
                // ocean value can be checked in hexagonconfig, currently 4
                return _Type == 4 || _Type == 16;
            }

            inline bool IsBorder(float2 uv) {
                return uv.x < (1.01 / 16.0) && uv.y < (1.01 / 16.0);
            }

            inline bool IsDecoration(float2 uv){
                return uv.y > (14.0 / 16.0);
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
                bool bIsAllowed = !IsBorder(IN.uv) && !IsDecoration(IN.uv);
                bool shouldChangeUV = IsWater() && bIsAllowed;
                o.uv = IN.uv + float2(shouldChangeUV ? lighting.x : 0, 0);
                // if we change the color too much, we suddenly declare this pixel as border!
                o.uv.x = bIsAllowed ? max(o.uv.x, 1.1 / 16.0) : o.uv.x;
                return o;
            }

            float3 HandleWaterDisplacement(float3 vertex, float2 uv){
                if (!IsWater() || IsBorder(uv) || IsDecoration(uv))
                    return vertex;
                    
                // displace ocean decoration vertices by their world location and time to create waves
                // only do this for the lighting calculation - don't actually displace the vertex!
                float TimeScale = 0.01;
                float WaveWidth = 20;
                float WaveHeight = 15;
                VertexPositionInputs VertexInput = GetVertexPositionInputs(vertex);
                float3 worldPos = VertexInput.positionWS;
                float4 worldFraction = float4(worldPos.xz / _WorldSize.xy, 0, 0);
                float4 noiseUV = float4(frac(worldFraction.xy * WaveWidth + _Time.y * TimeScale), 0, 0);
                float4 noise = tex2Dlod(_NoiseTex, noiseUV);
                return vertex + float3(0, noise.g * WaveHeight, 0);
            }

            v2g vert (appdata v)
            {
                v2g o;

                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_TRANSFER_INSTANCE_ID(v, o);
    
                VertexPositionInputs VertexInputs = GetVertexPositionInputs(v.vertex.xyz);
                
                // pass through in object space, since we have to use geometry shader to update the normal anyway
                o.vertex = v.vertex;
                o.normal = v.normal;
                o.uv = TRANSFORM_TEX(v.uv, _TypeTex);
            
                return o;
            }

            [maxvertexcount(3)]
            void geom(triangle v2g IN[3], inout TriangleStream<g2f> triStream){
                g2f o;
                
                float3 centerWorld = (IN[0].vertex + IN[1].vertex + IN[2].vertex).xyz / 3.0;
                centerWorld = GetVertexPositionInputs(centerWorld).positionWS;
    
                // since geometry shader has access to each triangle
                // we can calculate the ocean lighting here
                float3 AB = normalize(HandleWaterDisplacement(IN[1].vertex.xyz, IN[1].uv) - HandleWaterDisplacement(IN[0].vertex.xyz, IN[0].uv));
                float3 AC = normalize(HandleWaterDisplacement(IN[2].vertex.xyz, IN[2].uv) - HandleWaterDisplacement(IN[0].vertex.xyz, IN[0].uv));
                float3 triangleNormal = cross(AB, AC);
     

                [unroll]
                for (int i = 0; i < 3; i++)
                {
                    VertexNormalInputs NormalInputs = GetVertexNormalInputs(IN[i].normal);
                    VertexPositionInputs VertexInputs = GetVertexPositionInputs(IN[i].vertex.xyz);
                    o.vertexOS = IN[i].vertex.xyz;
                    o.vertexCS = VertexInputs.positionCS;
                    o.vertexWS = VertexInputs.positionWS;
                    o.normal = NormalInputs.normalWS;
                    o.centerWorld = centerWorld;
                    UNITY_TRANSFER_INSTANCE_ID(IN[i], o);
                    o = HandleLighting(IN[i], o, triangleNormal);
        
                    triStream.Append(o);
        
                }

                triStream.RestartStrip();
            }

            float4 painterly(g2f i)
            { 
                // compare to center of object to have better range 
                // also offset by world coordinates to avoid mirror'ing - but beware, y and z axis have bad influence!
                float3 pos = i.normal;
                float3 noise = ssnoise(pos, _NoiseScale, 0, 3, 2);
                float3 posNoise = pos * (1 - _NoiseMix) + noise * _NoiseMix;
                float3 vor = voronoi3(_Scale * posNoise) / _Scale;
    
                // bring from ~-1..0 (depends on object size) to 0..1 range for color mapping 
                vor = vor + 1;
                pos = pos + 1;
                float2 UV = GetUVForVoronoi(vor, pos);
    
                // uv has a range of ~-0.2..0.2 (depending on voronoi scale), bring to 0..1
                UV = (UV / (1 / _Scale * 2) + 0.5);
                float4 texData = tex2D(_BrushNormalTex, UV);
                float3 normal = texData.xyz;
                float3 alpha = texData.a;
    
                // feed the brush into the original pos to offset the voronoi by strokes
                float3 pos2 = alpha == 0 ? posNoise : blendLinearLight(posNoise, normal, _BrushVoronoiOffset);
                float3 vor2 = voronoi3(_Scale * pos2) / _Scale;
    
                // now use this new voronoi and add normal brushstrokes per se 
                float3 endNormal = alpha == 0 ? vor2 : vor2 * (1 - _BrushVoronoiMix) + normal * _BrushVoronoiMix;
                // bring back to 0..1
                endNormal = endNormal * 0.5 + 0.5;
    
                half painterlyNL = max(0, dot(endNormal, _MainLightPosition.xyz));
                return painterlyNL * _MainLightColor;
            }

            float4 getRegularColor(g2f i){
                // as we have a split uv map we need to wrap around
                int xType = _Type / 16.0;
                int yType = _Type % 16;
                float StandardColor = (i.uv.x * 16.0) + xType * 16 / 2.0;
                float u = StandardColor;
                float v = yType;
                float2 uv = float2(u, v) / 16.0;
                float4 color = tex2D(_TypeTex, uv);
                return color;
            }

            int getHighlight(g2f i){
            
                bool bIsHighlightable = IsBorder(i.uv) || IsDecoration(i.uv);
                if (!bIsHighlightable)
                    return 0;
                
                // use OS vertex angle for dashed outline
                if (_PreMalaised > 0){
                    float angle = degrees(acos(dot(normalize(i.vertexOS.xz), float2(1, 0))));
                    int stepped = ((int)(angle / 15) % 2) == 0 ? 1 : 0;
                    return stepped * 4;
                }

                return  _Malaised > 0 ? 4 :
                        _Selected > 0 ? 1 :
                        _Adjacent > 0 ? 3 :
                        _Hovered > 0 ? 2 :
                         0;
            }

            float4 getHighlightColor(int highlight){
                float HighlightColor = highlight - 1;
                float2 uv = float2(HighlightColor, 0) / 16.0;
                float4 color = tex2D(_TypeTex, uv);
                return color;
            }

            float4 getDecorationColor(g2f i){
                // since marking as decoration alreay relies on using the last slot for colors, we can
                // just return the uv's
                float4 color = tex2D(_TypeTex, i.uv);
                return color;
            }

            float4 getColorByType(g2f i)
            {
                // each uv stores information about hex type (uv.y) and model vertex type (uv.x) 
                // the color of each vertex is dependent on its own type (border, decoration, highlight,..)
                // as well as the type of hex itself (eg desert vs ocean)
                int highlight = getHighlight(i);
                bool isHighlighted = highlight > 0;

                float4 regularColor = getRegularColor(i);
                float4 decorationColor = getDecorationColor(i);
                float4 highlightColor = getHighlightColor(highlight);

                return isHighlighted ? highlightColor : 
                       IsDecoration(i.uv) ? decorationColor : 
                       regularColor;
            }

            half4 frag(g2f i) : SV_Target
            {
                float4 color = getColorByType(i);

                float4 painterlyEffect = painterly(i);
        
                // light influence
                float3 ambient = SampleSH(i.normal);
                color.xyz *= i.diff.xyz + ambient * 0.3;
                
    
                float Desaturated = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                color.rgb = float3(
                    lerp(color.r, Desaturated, _Desaturation),
                    lerp(color.g, Desaturated, _Desaturation),
                    lerp(color.b, Desaturated, _Desaturation)
                );
                color.a = 1;
    
                VertexPositionInputs vertexInput = (VertexPositionInputs)0;
                vertexInput.positionWS = i.vertexWS;
 
                float4 shadowCoord = GetShadowCoord(vertexInput);
                half shadowAttenutation = MainLightRealtimeShadow(shadowCoord);
                color = lerp(float4(0.01, 0.01, 0.03, 1), color, shadowAttenutation);
    
                return color;
            }
            ENDHLSL
        }

        // shadow casting support
        UsePass"Legacy Shaders/VertexLit/SHADOWCASTER"
    }
}
