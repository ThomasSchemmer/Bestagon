#ifndef INCLUDE_CLOUDNOISE
#define INCLUDE_CLOUDNOISE

#include "Assets/Materials/Shaders/Util/Util.cginc" // for map function

/** 
 * The whole data is compressed into a single int, so 4 noise textures with 8 bit each (0.255 value)
 */

float ConvertCloudNoiseSelectionToColor(int Data, int Selection){
    int Offset = (3 - Selection) * 8;
    int Value = (Data >> Offset) & 0xFF;
    return Value / 255.0;
}

float4 ConvertCloudNoiseToColor(int Data){
    float r = ConvertCloudNoiseSelectionToColor(Data, 0);
    float g = ConvertCloudNoiseSelectionToColor(Data, 1);
    float b = ConvertCloudNoiseSelectionToColor(Data, 2);
    float a = ConvertCloudNoiseSelectionToColor(Data, 3);
    return float4(r, g, b, a);
}

/** See Real-time rendering of volumetric clouds, Fredrik Häggström */
float GetDensityFromColor(float4 Color){
    float Value = Color.g * 0.625f + Color.b * 0.25f + Color.a * 0.125f;
    float alpha = map(Color.r, Value - 1, 1, 0, 1);
    return alpha;
}

float GetUnpackedDensity(int Data){
    float4 Color = ConvertCloudNoiseToColor(Data);
    return GetDensityFromColor(Color);
}

float3 WorldPosToNoisePerc(float3 WorldPos, float3 WorldMin, float3 WorldMax){
    return map(WorldPos, WorldMin, WorldMax, 0, 1);
}

int3 NoisePercToNoisePos(float3 NoisePerc, float ResolutionXZ, float ResolutionY){

    int xValue = round(NoisePerc.x * ResolutionXZ);
    int yValue = round(NoisePerc.y * (ResolutionY));
    yValue = ResolutionY - yValue;
    int zValue = round(NoisePerc.z * ResolutionXZ);
    int Offset = (zValue % 2 == 0) ? 1 : 0;
    zValue = zValue + Offset;
    return int3(xValue, yValue, zValue);
}

int NoisePosToNoiseIndex(int3 NoisePos, float ResolutionXZ, float ResolutionY){
    int Index = NoisePos.y * ResolutionXZ * ResolutionXZ + NoisePos.z * ResolutionXZ + NoisePos.x;
    return Index;
}

int3 OffsetNoisePos(int3 NoisePos, int3 Offset, float ResolutionXZ, float ResolutionY){
    int3 Copy = 0;
    Copy.x = min(max(0, NoisePos.x + Offset.x), ResolutionXZ);
    Copy.y = min(max(0, NoisePos.y + Offset.y), ResolutionY);
    Copy.z = min(max(0, NoisePos.z + Offset.z), ResolutionXZ);
    return Copy;
}

#endif // INCLUDE_CLOUDNOISE
            