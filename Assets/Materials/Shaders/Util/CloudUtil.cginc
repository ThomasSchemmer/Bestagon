#ifndef INCLUDE_CLOUDNOISE
#define INCLUDE_CLOUDNOISE

#include "Assets/Materials/Shaders/Util/Util.cginc" // for map function

/** 
* The whole functionality is shared between the actual clouds computing and the shadows for them
* Since the clouds have to be a postprocessing effect, but their shadows have to be done before that,
* the functionality is needed in two different types of shaders and cannot be in the same file
* (i think unity ignores post processing shadow casters)
*/

struct Varyings {
	float4 positionCS 	: SV_POSITION;
    float3 positionWS : TEXCOORD1;
	float2 uv		: TEXCOORD0;
};

/** See CloudRenderer data: each line of hex's is packed into these uints */
StructuredBuffer<uint> MalaiseBuffer;

uniform float4 _CameraPos, _CameraForward, _CameraUp, _CameraRight, _CameraExtent;
uniform int _bIsEnabled;
// x and y contain max global tile location 
uniform float4 _WorldSize;
uniform float4 _TileSize;
uniform float _NumberOfChunks;
uniform float _ResolutionXZ;
uniform float _ResolutionY;
uniform float _ChunkSize;

            
SAMPLER(_NoiseTex);

float _StepAmount;
float _CloudCutoff;
float _CloudHeightMin;
float _CloudHeightMax;
float _CloudDensityMultiplier;
float4 _CloudColor;
float4 _NoiseWeights;
float _WindSpeed;
float _WindCrossMulti;
    
float _LightAbsorptionTowardsSun;
float _LightAbsorptionThroughCloud;
float _DarknessThreshold;
float _LightStepAmount;
float4 _PhaseParams;

float4 _ShadowOffset;

float3 WorldPosToNoisePerc(float3 WorldPos, float3 WorldMin, float3 WorldMax){
    return map(WorldPos, WorldMin, WorldMax, 0, 1);
}

            
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

// Henyey-Greenstein
float hg(float a, float g) {
    float g2 = g*g;
    return (1-g2) / (4*3.1415*pow(1+g2-2*g*(a), 1.5));
}

float phase(float a) {
    float blend = .5;
    float hgBlend = hg(a, _PhaseParams.x) * (1 - blend) + hg(a, -_PhaseParams.y) * blend;
    return _PhaseParams.z + hgBlend * _PhaseParams.w;
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

bool IsLocationInClouds(float3 WorldLocation){
    int2 GlobalTileLocation = WorldSpaceToTileSpace(WorldLocation);

    int _IsValidLocation = IsValidLocation(GlobalTileLocation);
    if (_IsValidLocation == 0)
        return false;

    int _IsHexMalaised = IsHexAtLocationMalaised(GlobalTileLocation);
    if (_IsHexMalaised == 0)
        return false;

    return true;
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


float GetDensityForWorld(float3 WorldLocation, float4 _NoiseTex_ST, float4 _NoiseTex_TexelSize) {
    if (!IsLocationInClouds(WorldLocation))
        return 0;

    float3 NoisePerc = WorldPosToNoisePerc(WorldLocation, GetCloudsMin(), GetCloudsMax());
                
    // tile the coordinates according to texture settings (but leave y untiled, as its way smaller)
    // also unity expects the z coordinate in a 3D tex to be the "depth", but we have y 
    float3 TiledNoisePerc = NoisePerc;
    TiledNoisePerc.xz = TRANSFORM_TEX(NoisePerc.xz, _NoiseTex);
                
    // simplex noise (x part of the texture) does not tile, so we need to have a global UV coord
    float3 TimedOffset = float3(-1, -1, 0) * _Time.y * _WindSpeed;
    float3 TimedNoisePerc = NoisePerc.xzy + TimedOffset;
    TiledNoisePerc += TimedOffset * _WindCrossMulti;

    float4 Noise = 0;

    Noise.x = tex3Dlod(_NoiseTex, float4(TimedNoisePerc, 1)).x;
    Noise.yzw = tex3Dlod(_NoiseTex, float4(TiledNoisePerc.xzy, 1)).yzw;

    float4 normalizedNoiseWeights = _NoiseWeights / dot(_NoiseWeights, 1);
    float shapeFBM = dot(Noise, normalizedNoiseWeights);

    float Density = max(0, shapeFBM - _CloudCutoff) * _CloudDensityMultiplier;
    return Density;
}

float GetLightIntensityForWorld(float3 WorldPos, float4 _NoiseTex_ST, float4 _NoiseTex_TexelSize){
    float3 LightDir = _MainLightPosition.xyz;
    float2 Box = RayBoxDist(WorldPos, LightDir);
                
    float StepLength = Box.y / _LightStepAmount;

    float LightSum = 0;
    for (int i = 0; i < _LightStepAmount; i++){
        float3 Pos = WorldPos + i * StepLength * LightDir;
        float Density = GetDensityForWorld(Pos, _NoiseTex_ST, _NoiseTex_TexelSize);
        LightSum += Density * StepLength;
    }

    float transmittance = exp(-LightSum * _LightAbsorptionTowardsSun);
    return _DarknessThreshold + transmittance * (1 - _DarknessThreshold);
}

float4 GetCloudColorForPixel(float3 PositionWorld, float4 _NoiseTex_ST, float4 _NoiseTex_TexelSize, float4 OriginalColor){
    // check if pixel is looking at cloud container (big box, implicitly defined)
    
    float2 Box = RayBoxDist(PositionWorld, _CameraForward.xyz);
    if (Box.y == 0)
        return 0;

        
#ifndef ENABLE_LIGHT_PASS
    _StepAmount /= 3;
#endif

    float3 BoxStartWorld = PositionWorld + _CameraForward.xyz * Box.x;
    float3 BoxEndWorld = PositionWorld + _CameraForward.xyz * (Box.x + Box.y);
    float3 BoxDir = normalize(BoxEndWorld - BoxStartWorld);
    float BoxDistance = distance(BoxStartWorld, BoxEndWorld);
    float StepLength = BoxDistance / _StepAmount;

    float Transmittance = 1;

#ifdef ENABLE_LIGHT_PASS
    float cosAngle = dot(BoxDir, _MainLightPosition.xyz);
    float phaseVal = phase(cosAngle);

    float LightEnergy = 0;
    for (int i = 0; i < _StepAmount; i++){
        float3 Pos = BoxStartWorld + i * StepLength * BoxDir;
        float Density = GetDensityForWorld(Pos, _NoiseTex_ST, _NoiseTex_TexelSize) * StepLength;

        if (Density <= 0)
            continue;
                        
        float Light = GetLightIntensityForWorld(Pos, _NoiseTex_ST, _NoiseTex_TexelSize);
        LightEnergy += Density * Light * Transmittance * phaseVal;
        Transmittance *= exp(-Density * _LightAbsorptionThroughCloud);
        if (Transmittance < 0.01)
            break;
    }
    
    return float4(OriginalColor.xyz * Transmittance + LightEnergy * _CloudColor.xyz, 1);

#else 
    for (int i = 0; i < _StepAmount; i++){
        float3 Pos = BoxStartWorld + i * StepLength * BoxDir;
        float Density = GetDensityForWorld(Pos, _NoiseTex_ST, _NoiseTex_TexelSize) * StepLength;
        if (Density <= 0)
            continue;

        Transmittance *= exp(-Density);
    }

    return 1 - Transmittance;
#endif // ENABLE_LIGHT_PASS
}

// https://forum.unity.com/threads/urp-opacity-and-shadows-is-this-even-supported-anymore.1045252/
half dither(half2 uv, half alpha)
{
    const half DITHER_THRESHOLDS[16] =
    {
        1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
        13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
        4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
        16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
    };
    uint index = (uint(uv.x) % 4) * 4 + uint(uv.y) % 4;
    return alpha - DITHER_THRESHOLDS[index];
}

#endif // INCLUDE_CLOUDNOISE
            