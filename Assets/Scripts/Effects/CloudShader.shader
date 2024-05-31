/** 
 * Shader to display volumetric clouds for malaised hexes
 * Works in multiple steps:
 * - take screenspace rectangle pixel to uv position 
 * - Raycast into the world to find overlap with cloud layer (box)
 * - Check if the position in that box is for a hexagon (map world space to hex space)
 * - Check if hexagon is malaised (MalaiseBuffer, bitbanged)
 * - Raymarch through the box for a malaised hexes
 * - check cloud density (aka layered noise) for each ray step
 */
Shader "Custom/CloudShader"
{
    Properties
    {
        _NoiseTex ("NoiseTex", 3D) = "white" {}
        _StepAmount ("StepAmount", Range(1, 5)) = 1
        _CloudHeightMin("CloudHeightMin", Float) = 10
        _CloudHeightMax("CloudHeightMax", Float) = 15
        _CloudCutoff ("CloudCutoff", Range(0, 1)) = 0.5
        _CloudDensityMultiplier("CloudDensityMultiplier", Range(1, 100)) = 50
        _ShowIndex("ShowIndex", Float) = 0
    }


    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)
    
    float4 _NoiseTex_ST;
    float4 _NoiseTex_TexelSize;
    uniform float4 _CameraPos, _CameraForward, _CameraUp, _CameraRight, _CameraExtent;
    uniform int _bIsEnabled;
    // x and y contain max global tile location 
    uniform float4 _WorldSize;
    uniform float _ChunkSize;
    uniform float4 _TileSize;
    uniform float _NumberOfChunks;
    uniform float _ResolutionXZ;
    uniform float _ResolutionY;
    float _StepAmount;
    float _CloudCutoff;
    float _CloudHeightMin;
    float _CloudHeightMax;
    float _CloudDensityMultiplier;
    float _ShowIndex;

    CBUFFER_END
    ENDHLSL

    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline" = "UniversalPipeline" "Queue"="Transparent"}
        LOD 100

        Cull Off

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Common.hlsl"
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Assets/Materials/Shaders/Util/Util.cginc"
            #include "Assets/Materials/Shaders/Util/CloudNoise.cginc"

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            
            #pragma enable_d3d11_debug_symbols //renderdoc
            
            TEXTURE2D_X(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);

            /** See CloudRenderer data: each line of hex's is packed into these uints */
            StructuredBuffer<uint> MalaiseBuffer;
            /** Contains 3D noise textures for actual cloud density computation, compressed into int's */
            //StructuredBuffer<int> CloudNoiseBuffer;

            struct Attributes {
                uint vertexID : SV_VertexID;
			};

			struct Varyings {
				float4 positionCS 	: SV_POSITION;
				float2 uv		: TEXCOORD0;
			};
            
            SAMPLER(_NoiseTex);
            
            float3 GetPointOnPlane(float3 PlaneOrigin, float3 PlaneNormal, float3 Origin, float3 Direction)
            {
                float A = dot(PlaneOrigin - Origin, PlaneNormal);
                float B = dot(Direction, PlaneNormal);
                float d = A / B;
                return Origin + Direction * d;
            }

            /** Conversion functions from RedBlobGames. God i love their resources */
            int2 RoundToAxial(float x, float y) {
                int xgrid = round(x);
                int ygrid = round(y);
                x -= xgrid;
                y -= ygrid;
                int dx = round(x + 0.5 * y) * (x * x >= y * y);
                int dy = round(y + 0.5 * x) * (x * x < y * y);
                return int2(xgrid + dx, ygrid + dy);
            }

            int2 AxialToOffset(int2 hex){
                int col = hex.x + (hex.y - (hex.y&1)) / 2.0;
                int row = hex.y;
                return int2(col, row);
            }

            int2 WorldSpaceToTileSpace(float3 WorldSpace){
                float q = (sqrt(3) / 3.0 * WorldSpace.x  -  1./3 * WorldSpace.z) / 10;
                float r = (2./3 * WorldSpace.z) / 10;
                return AxialToOffset(RoundToAxial(q, r));
            }

            int4 TileSpaceToHexSpace(int2 TileSpace){
                int ChunkX = (int)(TileSpace.x / _ChunkSize);
                int ChunkY = (int)(TileSpace.y / _ChunkSize);
                int HexX = (int)(TileSpace.x - ChunkX * _ChunkSize);
                int HexY = (int)(TileSpace.y - ChunkY * _ChunkSize);
                return int4(ChunkX, ChunkY, HexX, HexY);
            }

            /** Checks the corresponding bit position in the buffer */
            uint IsHexAtLocationMalaised(int2 GlobalTileLocation){
                int HexIndex = GlobalTileLocation.y * _ChunkSize * _NumberOfChunks + GlobalTileLocation.x;
                
                int IntIndex = HexIndex / 32.0; 
                int IntRemainder = (HexIndex - IntIndex * 32); 
               
                int ByteIndex = IntRemainder / 8.0; 
                int BitIndex = IntRemainder - ByteIndex * 8; 

                uint IntValue = MalaiseBuffer[IntIndex]; 
                uint ByteValue = ((IntValue >> ((3 - ByteIndex) * 8)) & 0xFF); 
                uint BitValue = (ByteValue >> (7 - BitIndex)) & 0x1;
                
                return BitValue;
            }

            int IsValidLocation(int2 GlobalTileLocation){
                int IsHex = 
                    _bIsEnabled == 1 &&
                    GlobalTileLocation.x >= 0 &&
                    GlobalTileLocation.y >= 0 &&
                    GlobalTileLocation.x <= _WorldSize.x &&
                    GlobalTileLocation.y <= _WorldSize.y;
                return IsHex;
            }

            int Equals(int4 A, int4 B){
                return A.x == B.x && A.y == B.y && A.z == B.z && A.w == B.w;
            }

            float3 GetCloudsMin(){
                return float3(-_TileSize.x, _CloudHeightMin, -_TileSize.z);
            }

            float3 GetCloudsMax(){
                return float3(_WorldSize.x * _TileSize.x * 2 + _TileSize.x, _CloudHeightMax, _WorldSize.y * _TileSize.z * 2 + _TileSize.z);
            }

            float GetCloudHeight(){
                return _CloudHeightMax - _CloudHeightMin;
            }

            float2 RayBoxDist(float3 RayOrigin, float3 RayDir){
                // adapted from sebastian lague
                // slightly extend the box since the hexagons are center positioned
                float3 BoundsMin = GetCloudsMin();
                float3 BoundsMax = GetCloudsMax();

                float3 T0 = (BoundsMin - RayOrigin) / RayDir;
                float3 T1 = (BoundsMax - RayOrigin) / RayDir;
                float3 TMin = min(T0, T1);
                float3 TMax = max(T0, T1);

                float DistA = max(max(TMin.x, TMin.y), TMin.z);
                float DistB = min(TMax.x, min(TMax.y, TMax.z));
    
                float DistToBox = max(0, DistA);
                float DistInsideBox = max(0, DistB - DistToBox);
                return float2 (DistToBox, DistInsideBox);
            }

            float GetDensityForWorld(float3 WorldLocation) {
                // prolly remove ifs
                int2 GlobalTileLocation = WorldSpaceToTileSpace(WorldLocation);
                //int4 Location = TileSpaceToHexSpace(GlobalTileLocation);

                int _IsValidLocation = IsValidLocation(GlobalTileLocation);
                if (_IsValidLocation == 0)
                    return 0;

                int _IsHexMalaised = IsHexAtLocationMalaised(GlobalTileLocation);
                //if (_IsHexMalaised == 0)
                //    return 0;

                float3 NoisePerc = WorldPosToNoisePerc(WorldLocation, GetCloudsMin(), GetCloudsMax());
                //int3 NoisePos = NoisePercToNoisePos(NoisePerc, _ResolutionXZ, _ResolutionY);
                //int NoiseIndex = NoisePosToNoiseIndex(NoisePos, _ResolutionXZ, _ResolutionY);
                //float Density = GetUnpackedDensity(CloudNoiseBuffer[NoiseIndex]);

                NoisePerc.xz = TRANSFORM_TEX(NoisePerc.xz, _NoiseTex);
                // unity expects the z coordinate in a 3D tex to be the "depth", but we have y 
                float4 Noise = tex3Dlod(_NoiseTex, float4(NoisePerc.xzy, 1));
                
                if (_ShowIndex == 1)
                    return Noise.r;
                if (_ShowIndex == 2)
                    return Noise.g;
                if (_ShowIndex == 3)
                    return Noise.b;
                if (_ShowIndex == 4)
                    return Noise.a;

                float Density = GetDensityFromColor(Noise);

                Density = max(0, Density - _CloudCutoff) * _CloudDensityMultiplier;
                return Density;
            }

            /** create a fullscreen rect out of thin air */
            Varyings vert(Attributes i) {
				Varyings OUT;
                
                float4 pos = GetFullScreenTriangleVertexPosition(i.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(i.vertexID);
				OUT.positionCS = pos;
				OUT.uv = uv;
				return OUT;
			}

            half4 frag(Varyings input) : SV_Target
            { 
            return 0;
                float d = SampleSceneDepth(input.uv);
                float2 UV = (input.uv - 0.5) * 2;
                float3 PositionWorld = _CameraPos.xyz + UV.x * _CameraRight.xyz + UV.y * _CameraUp.xyz + 100 * -_CameraForward.xyz;
                float2 Box = RayBoxDist(PositionWorld, _CameraForward.xyz);

                if (Box.y == 0)
                    return 0;

                float3 BoxStartWorld = PositionWorld + _CameraForward.xyz * Box.x;
                float3 BoxEndWorld = PositionWorld + _CameraForward.xyz * (Box.x + Box.y);
                float3 BoxDir = normalize(BoxEndWorld - BoxStartWorld);
                float BoxDistance = distance(BoxStartWorld, BoxEndWorld);
                float StepLength = BoxDistance / _StepAmount;

                float Sum = 0;
                for (int i = 0; i < _StepAmount; i++){
                    float3 Pos = BoxStartWorld + i * StepLength * BoxDir;
                    float Density = GetDensityForWorld(Pos);
                    Sum += Density / _StepAmount;
                }
                
                float Transmittance = exp(-Sum);

                if (_ShowIndex == 0){
                    float4 OriginalColor = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, input.uv);
                    return Transmittance * OriginalColor + (1 - Transmittance) * float4(0, 0, 0, 1);
                }
                return Sum;


                /*                
                tex2D(_TypeTex, uv);
                return color;
                */
            }
            ENDHLSL
        }
    }
}
