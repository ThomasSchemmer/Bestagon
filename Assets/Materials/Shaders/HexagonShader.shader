﻿/** 
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

*/

Shader"Custom/HexagonShader"
{
    Properties
    {
//also contains instanced props
        _TypeTex ("Types", 2D) = "white" {}
        _NoiseTex("Noise", 2D) = "white" {}

        [Header(Painterly)][Space]
        _NormalNoiseScale("Normal Noise Scale", Range(10, 40)) = 20
        _NormalNoiseEffect("Normal Noise Effect", Range(0, 0.3)) = 0.1
        _VoronoiScale ("Voronoi Scale", Range(0, 10)) = 6
        _EdgeBlendFactor("Edge Blend Factor", Range(0, 0.3)) = 0.1
        _CenterBlendFactor("Center Blend Factor", Range(0, 0.3)) = 0.1
        _BrushNormalTex("Brush Texture", 2D) = "white" {}

        [Header(Globals)][Space]
        _WorldSize("World Size", Vector) = (0, 0, 0, 0)
        _Desaturation("Desaturation", Float) = 0
        
        [Header(UsableWith)][Space]
        _UnusableDesaturation("Unusable Desaturation", Float) = 0
        _UsableOnMask("UsableOn Mask", Int) = 0
        _AdjacentWithMask("AdjacentWith Mask", Int) = 0
        _CheckUsable("Check Usable", Int) = 0
            
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
        
        float _UnusableDesaturation;
        int _UsableOnMask;
        int _AdjacentWithMask;
        int _CheckUsable;

        //painterly
        float _VoronoiScale;
        float _NormalNoiseScale;
        float _NormalNoiseEffect;
        float _EdgeBlendFactor;
        float _CenterBlendFactor;
        float4 _BrushNormalTex_ST;
        float4 _BrushNormalTex_TexelSize;
    CBUFFER_END
    
        // implicit state enum
        static uint DefaultState = 0;
        static uint HoveredState = 1 << 0;
        static uint SelectedState = 1 << 1;
        static uint PreMalaisedState = 1 << 2;
        static uint MalaisedState = 1 << 3;
        static uint AdjacentState = 1 << 4;
        static uint ReachableState = 1 << 5;
        static uint AoEAffectedState = 1 << 6;

        // describes the AoE outline indices per surrounding hex
        static int BorderIndices[] = {
            0, 1, 5,
            0, 1, 2,
            1, 2, 3,
            2, 3, 4,
            3, 4, 5,
            4, 5, 0
        };
    ENDHLSL

    SubShader
    {
        LOD 100
                
        /************************ BEGIN HEX SHADER *************************/
        
        Tags { "RenderType"="Opaque" "RenderPipeline" = "UniversalPipeline" "Queue"="Geometry" "LightMode" = "UniversalForward"}
        
        Pass 
        {
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" // shadows and ambient light

            #include "Assets/Materials/Shaders/Util/Util.cginc" //for snoise
            #include "Assets/Materials/Shaders/Util/Painterly.cginc" 

            #define PI 3.14159265359
            static float i60 = 1 / 60.0;

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
                float3 vertexOS : TEXCOORD3;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            // shared between all hexagons, lookup img for colors
            SAMPLER(_TypeTex);
            // noise texture for easier lookup / shifting water
            SAMPLER(_NoiseTex);
            SAMPLER(_BrushNormalTex);

            // each hexagon mesh sets these values for itself
            // hexes can be at the same time selected and pre malaised and AoE affected
            UNITY_INSTANCING_BUFFER_START(Props)
                UNITY_DEFINE_INSTANCED_PROP(float, _Type)
                UNITY_DEFINE_INSTANCED_PROP(uint, _State)
                UNITY_DEFINE_INSTANCED_PROP(uint, _Discovery)
                UNITY_DEFINE_INSTANCED_PROP(float4, _SourceLocation)
            UNITY_INSTANCING_BUFFER_END(Props)

            inline bool IsVisited() {
                // can be check in HexagonData.DiscoveryState
                return _Discovery == 2;
            }

            inline bool IsWater(){
                // ocean values can be checked in hexagonconfig
                return _Type == 4 || _Type == 16;
            }

            inline bool IsBorder(float2 uv) {
                return uv.x < 0.063125 && uv.y < 0.063125; // 1.01 / 16
            }

            inline bool IsDecoration(float2 uv){
                return uv.y > 0.875;        // 14 / 16
            }

            inline bool Is(uint Flag){
                return (_State & Flag) > 0;
            }

            float4 NormalLighting(float3 normal){
                // while non-water tiles use directional lighting from "sun light",
                // water tiles have to lerp between the prescribed color values in the tiles.png
                // to pass this to the fragment shader simply write it in diff.x and add to uv.x

                // range 0..1
                half nl = max(0, dot(normal, _MainLightPosition.xyz));

                if (IsWater()){
                    // needs to be 0..3/16
                    nl = nl * 0.1875;
                    return float4(nl, 0, 0, 0);
                }else{
                    return nl * _MainLightColor;
                }
            }

            g2f HandleLighting(v2g IN, g2f o, float3 waterNormal){
                //diffuse lighting calculations
                VertexNormalInputs NormalInputs = GetVertexNormalInputs(IN.normal);
                float3 normal = IsWater() ? waterNormal : NormalInputs.normalWS;
                float4 lighting = NormalLighting(normal);
                o.diff = IsWater() ? 1 : lighting;
                bool bIsAllowed = !IsBorder(IN.uv) && !IsDecoration(IN.uv);
                bool shouldChangeUV = IsWater() && bIsAllowed;
                o.uv = IN.uv + float2(shouldChangeUV ? lighting.x : 0, 0);
                // if we change the color too much, we suddenly declare this pixel as border!
                o.uv.x = bIsAllowed ? max(o.uv.x, 0.06875) : o.uv.x;
                return o;
            }

            float3 HandleWaterDisplacement(float3 vertex, float2 uv){
                if (!IsWater() || IsBorder(uv) || IsDecoration(uv) || !IsVisited())
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
                    UNITY_TRANSFER_INSTANCE_ID(IN[i], o);
                    o = HandleLighting(IN[i], o, triangleNormal);
        
                    triStream.Append(o);
        
                }

                triStream.RestartStrip();
            }


            float4 getRegularColor(g2f i){
                // as we have a split uv map we need to wrap around
                int xType = _Type / 16.0;
                int yType = _Type % 16;
                float StandardColor = (i.uv.x * 16.0) + xType * 8.0;
                float u = StandardColor;
                float v = yType;
                float2 uv = float2(u, v) / 16.0;
                float4 color = tex2D(_TypeTex, uv);
                return color;
            }

            float GetAngle(float2 Position){
                return degrees(acos(dot(normalize(Position), float2(1, 0))));
            }

            float GetSignedAngle(float2 Position){
                float Angle = -atan2(Position.y, Position.x) + radians(30);
                Angle += Angle < 0 ? 2 * PI : 0;
                Angle = degrees(Angle);
                return Angle;
            }

            float GetOuterHullHighlight(g2f i){
                // hex position relative to the source determines which borders should  
                // be highlighted
                VertexPositionInputs VertexInput = GetVertexPositionInputs(0);
                float2 ToSelf = VertexInput.positionWS.xz - _SourceLocation.zw;

                float Dis = length(ToSelf);
                bool bIsSource = Dis < 0.1;
                bool bIsHighlightable = IsBorder(i.uv) && !bIsSource;
                if (!bIsHighlightable)
                    return 0;
                
                float BorderAngle = GetSignedAngle(i.vertexOS.xz);
                float HexAngle = GetSignedAngle(ToSelf);
                int HexIndex = HexAngle * i60;
                int BorderIndex = BorderAngle * i60;

                bool bIsOuterBorder = 
                    BorderIndices[HexIndex * 3 + 0] == BorderIndex ||
                    BorderIndices[HexIndex * 3 + 1] == BorderIndex ||
                    BorderIndices[HexIndex * 3 + 2] == BorderIndex;
                if (!bIsOuterBorder)
                    return 0;
                
                // invert to mix with pre-malaised 
                int stepped = 1 - ((uint)(BorderAngle / 15) % 2);
                return stepped * 2;
            }

            int getHighlight(g2f i){
            
                bool bIsHighlightable = IsBorder(i.uv) || IsDecoration(i.uv);
                if (!bIsHighlightable)
                    return 0;
                    
                int Value = 0;

                // use OS vertex angle for dashed outline
                if (Is(PreMalaisedState)){
                    float angle = GetAngle(i.vertexOS.xz);
                    int stepped = (uint)(angle / 15) % 2;
                    Value = stepped * 4;
                }

                // use border lookup for outer hull
                if (Value == 0 && Is(AoEAffectedState)){
                    Value = GetOuterHullHighlight(i);
                }

                // allow hovering etc to overwrite partial states
                Value = Value != 0 ? Value :
                    Is(MalaisedState) ? 4 :
                    Is(SelectedState) ? 1 :
                    Is(HoveredState) ? 2 :
                    Is(ReachableState) || Is(AdjacentState) ? 3 :
                    0;

                return Value;
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

            painterlyInfo getPainterlyInfo(g2f i){
                painterlyInfo info;
                info.normal = i.normal;
                info.vertexWS = i.vertexWS;
                info.vertexOS = i.vertexOS;
                info._NormalNoiseScale = _NormalNoiseScale;
                info._NormalNoiseEffect = _NormalNoiseEffect;
                info._VoronoiScale = _VoronoiScale;
                info._EdgeBlendFactor = _EdgeBlendFactor;
                info._CenterBlendFactor = _CenterBlendFactor;
                return info;
            }

            bool IsUnusable(){
                int Type = (int)_Type - 1;
                bool bIsUsableOn = (_UsableOnMask & (1 << Type)) > 0;
                bool bIsAdjacent = (_AdjacentWithMask & (1 << Type)) > 0;
                bool bCheckForUsable = _CheckUsable > 0;
                return bCheckForUsable && !bIsUsableOn && !bIsAdjacent;
            }

            half4 frag(g2f i) : SV_Target
            {
                painterlyInfo info = getPainterlyInfo(i);
                float2 UV = painterlyUV(info);

                float4 baseColor = getColorByType(i);
                float3 painterlyNormal = painterly(getPainterlyInfo(i), _BrushNormalTex);
                float4 painterlyLight = max(0, dot(painterlyNormal, _MainLightPosition.xyz)) * _MainLightColor;
        
                // light influence (both normal-based and ambient)
                float4 diffuseLight = float4(i.diff.xyz, 1);
                float4 ambientLight = float4(SampleSH(i.normal), 1);
                float4 globalLight = diffuseLight + ambientLight * 0.6; 

                float4 color = baseColor * painterlyLight * globalLight;
    
                float Desaturated = dot(color.rgb, float3(0.2126, 0.7152, 0.0722));
                float TargetDesaturation = IsUnusable() ? _UnusableDesaturation : _Desaturation;
                color.rgb = float3(
                    lerp(color.r, Desaturated, TargetDesaturation),
                    lerp(color.g, Desaturated, TargetDesaturation),
                    lerp(color.b, Desaturated, TargetDesaturation)
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
        
        /************************ END HEX SHADER *************************/

        // shadow casting support
        UsePass"Legacy Shaders/VertexLit/SHADOWCASTER"

        // depth passes
        UsePass "Universal Render Pipeline/Lit/DEPTHONLY"
        UsePass "Universal Render Pipeline/Lit/DEPTHNORMALS"
    }
}
