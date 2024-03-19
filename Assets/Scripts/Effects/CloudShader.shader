Shader"Custom/CloudShader"
{
    Properties
    {
    }


    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

    CBUFFER_START(UnityPerMaterial)

uniform float4 _CameraPos, _CameraForward, _CameraUp, _CameraRight, _CameraExtent;
uniform int _bIsEnabled;
uniform float4 _WorldSize;
uniform float _ChunkSize;
uniform float4 _TileSize;
uniform float _NumberOfChunks;

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

            #pragma vertex vert
            #pragma fragment frag
            #pragma target 5.0
            
            #pragma enable_d3d11_debug_symbols //renderdoc

            TEXTURE2D_X(_CameraColorTexture);
            SAMPLER(sampler_CameraColorTexture);

            /** See CloudRenderer data: each line of hex's is packed into these uints */
            StructuredBuffer<uint> MalaiseBuffer;

            struct Attributes {
                uint vertexID : SV_VertexID;
			};

			struct Varyings {
				float4 positionCS 	: SV_POSITION;
				float2 uv		: TEXCOORD0;
			};
            
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

            uint IsHexAtLocationMalaised(int2 GlobalTileLocation){
                int HexIndex = GlobalTileLocation.y * _ChunkSize * _NumberOfChunks + GlobalTileLocation.x;
                // 50
                int IntIndex = HexIndex / 32.0; // 1
                int IntRemainder = (HexIndex - IntIndex * 32); // 18
                
                int ByteIndex = IntRemainder / 8.0; // 2
                int BitIndex = IntRemainder - ByteIndex * 8; // 2

                uint IntValue = MalaiseBuffer[IntIndex]; // 3758211264
                uint ByteValue = ((IntValue >> ((3 - ByteIndex) * 8)) & 0xFF); // 192
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

            // sebastian lague
            float2 RayBoxDist(float3 RayOrigin, float3 RayDir){
                // slightly extend the box since the hexagons are center positioned
                float3 BoundsMin = float3(-_TileSize.x, 10, -_TileSize.z);
                float3 BoundsMax = float3(_WorldSize.x * _TileSize.x * 2 + _TileSize.x, 15, _WorldSize.y * _TileSize.z * 2 + _TileSize.z);

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

            Varyings vert(Attributes i) {
				Varyings OUT;
                
                float4 pos = GetFullScreenTriangleVertexPosition(i.vertexID);
                float2 uv  = GetFullScreenTriangleTexCoord(i.vertexID);
				OUT.positionCS = pos;
				OUT.uv = uv;
				return OUT;
			}

            half4 frag(Varyings i) : SV_Target
            { 
                return 0;
                float d = SampleSceneDepth(i.uv);

                float2 UV = (i.uv - 0.5) * 2;
                float3 PositionWorld = _CameraPos.xyz + UV.x * _CameraRight.xyz + UV.y * _CameraUp.xyz + 100 * -_CameraForward;
                float2 Box = RayBoxDist(PositionWorld, _CameraForward);
                return Box.y;
                if (Box.y == 0)
                    return 0;


                return 1;

                /*
                float3 IntersectionA = GetPointOnPlane(float3(0, 15, 0), float3(0, 1, 0), PositionWorld, _CameraForward.xyz);
                float3 IntersectionB = GetPointOnPlane(float3(0, 20, 0), float3(0, 1, 0), PositionWorld, _CameraForward.xyz);
                float2 GlobalTileLocationA = WorldSpaceToTileSpace(IntersectionA);
                float2 GlobalTileLocationB = WorldSpaceToTileSpace(IntersectionB);
                int4 LocationA = TileSpaceToHexSpace(GlobalTileLocationA);
                int4 LocationB = TileSpaceToHexSpace(GlobalTileLocationB);
                
                int _IsValidLocationA = IsValidLocation(GlobalTileLocationA);
                int _IsValidLocationB = IsValidLocation(GlobalTileLocationB);

                if (Equals(LocationA, LocationB) && _IsValidLocationB)
                    return float4(hash3(LocationB.zwx), 1);

                if (_IsValidLocationB == 1)
                    return float4(hash3(LocationB.zwx), 1);

                if (_IsValidLocationA == 1)
                    return float4(hash3(LocationA.zwx), 1);
                
                float4 color = SAMPLE_TEXTURE2D_X(_CameraColorTexture, sampler_CameraColorTexture, i.texcoord);
                return color;
                */
            }
            ENDHLSL
        }
    }
}
