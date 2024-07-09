#ifndef INCLUDE_PAINBTERLY
#define INCLUDE_PAINBTERLY
            
#include "Assets/Materials/Shaders/Util/Util.cginc" //for hash3
#include "Assets/Materials/Shaders/Util/BlendModes.cginc" 


float3 ProjectPositionOntoVoronoiPlane(float3 Center, float3 Position)
{
    float3 PlaneOrigin = Center;
    float3 PlaneNormal = normalize(Center);
    float3 VectorToPosition = Position - PlaneOrigin;
    float DistanceAlongNormal = dot(VectorToPosition, PlaneNormal);
    float3 ProjectedPosition = Position - DistanceAlongNormal * PlaneNormal;
    return ProjectedPosition;
}

/** See eg https://en.wikibooks.org/wiki/Linear_Algebra/Orthogonal_Projection_Onto_a_Line */
float GetDistanceAlongLine(float3 A, float3 B, float3 Position)
{
    float3 v = Position - A;
    float3 s = B - A;
    float c = dot(v, s) / dot(s, s);
    return c;
}

/**
 * Creates a new coordinate system on the plane created around the center normal
 * Returns the UV coordinates of the position mapped into this plane
 */
float2 GetUVForVoronoi(float3 Center, float3 Position)
{
    // use a random, but fixed offset to get the first axis (resulting in the same coordinate space per same center)        
    float3 RandomDirection = normalize(hash3(Center));
    // make sure they are not aligned, as this would kill cross product (can still happen, but veeery rarely)
    RandomDirection = dot(normalize(Center), RandomDirection) == 1 ? float3(1, 0, 0) : RandomDirection;

    float3 XPos = ProjectPositionOntoVoronoiPlane(Center, Center + RandomDirection);
    float3 XAxis = normalize(XPos - Center);
    float3 YAxis = normalize(cross(XAxis, Center));

    // since the projected position is on the plane created by the two axis, we can get its uv coordinates
    // by mapping onto each of the axis
    float3 ProjectedPosition = ProjectPositionOntoVoronoiPlane(Center, Position);
    float u = GetDistanceAlongLine(Center, Center + XAxis, ProjectedPosition);
    float v = GetDistanceAlongLine(Center, Center + YAxis, ProjectedPosition);
    return float2(u, v);
}

struct painterlyInfo{
    float3 normal;
    float3 vertexWS;
    float3 vertexOS;

    float _NormalNoiseScale;
    float _NormalNoiseEffect;
    float _VoronoiScale;
    float _EdgeBlendFactor;
    float _CenterBlendFactor;
};

float3 painterlyNormal(painterlyInfo i){
    // offset normals by world coordinates to avoid having flat surfaces (would lead to uniform outcome)
    float3 normal = clamp(i.normal, -1, 1);
    float worldSpaceNoise = snoise(i.vertexWS.xz / _NormalNoiseScale);

    float3 randomNormal = (1 - _NormalNoiseEffect) * normal + i.vertexOS * worldSpaceNoise * _NormalNoiseEffect;
    randomNormal = clamp(randomNormal, -1, 1);
    return randomNormal;
}

float2 painterlyUV(painterlyInfo i){
    // in general: make sure that the values do not go out of range too much, so that clamping them 
    // is not affecting them too much. Depends mostly on object size, adapt with NormalNoiseEffect

    float3 randomNormal = painterlyNormal(i);

    // map normal to a close-by voronoi to fake centers
    // cant use regular triangle centers as they would be too regular for round surfaces
    float3 vor = voronoi3(_VoronoiScale * randomNormal) / _VoronoiScale;
    vor = clamp(vor, -1, 1);
                
    // uv has a range of ~-0.2..0.2 (depending on voronoi scale), bring to 0..1
    float2 UV = GetUVForVoronoi(vor, randomNormal) * _VoronoiScale;
    UV = (UV / 2.0) + 0.5;
    UV = clamp(UV, 0, 1);
    return UV;
}

float3 painterly(painterlyInfo i, sampler2D _Tex)
{ 
    float2 UV = painterlyUV(i);
    float3 randomNormal = painterlyNormal(i);
                    
    float4 texData = tex2D(_Tex, UV);
    float3 brushNormal = texData.xyz;
    float alpha = texData.a;
    
    // feed the brush into the original pos to offset the voronoi by strokes (stroking the edges)
    float3 blendedNormal = alpha == 0 ? randomNormal : blendLinearLight(randomNormal, brushNormal, _EdgeBlendFactor);
    float3 blendedVoronoi = voronoi3(_VoronoiScale * blendedNormal) / _VoronoiScale;
                
    // now use this new voronoi and add normal brushstrokes per se (stroking the centers)
    float3 endNormal = alpha == 0 ? blendedVoronoi : blendedVoronoi * (1 - _CenterBlendFactor) + brushNormal * _CenterBlendFactor;

    // bring back to 0..1
    endNormal = endNormal * 0.5 + 0.5;
    endNormal = clamp(endNormal, 0, 1);
    return endNormal;
}

#endif // INCLUDE_PAINTERLY