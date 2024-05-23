#ifndef INCLUDE_PAINBTERLY
#define INCLUDE_PAINBTERLY
            
#include "Assets/Materials/Shaders/Util/Util.cginc" //for hash3

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

#endif // INCLUDE_PAINTERLY