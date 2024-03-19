#ifndef INCLUDE_PAINBTERLY
#define INCLUDE_PAINBTERLY
            
#include "Assets/Materials/Shaders/Util/Util.cginc" //for hash3

float3 ProjectPositionOntoVoronoiPlane(float3 VoronoiVertex, float3 Position)
{
    float3 PlaneOrigin = VoronoiVertex;
    float3 PlaneNormal = normalize(VoronoiVertex);
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
 * Creates a new coordinate system on the plane created by the voronoi vertex normal
 * Returns the UV coordinates of the position mapped into this plane
 */
float2 GetUVForVoronoi(float3 VoronoiVertex, float3 Position)
{
    // use a random, but fixed, offset to get the first axis        
    float3 Offset = hash3(VoronoiVertex);
    Offset = dot(normalize(VoronoiVertex), normalize(Offset)) == 1 ? float3(1, 0, 0) : Offset;
    float3 XPos = ProjectPositionOntoVoronoiPlane(VoronoiVertex, VoronoiVertex + Offset);
    float3 XAxis = normalize(XPos - VoronoiVertex);
    float3 YAxis = normalize(cross(XAxis, VoronoiVertex));

    // since the projected position is on the plane created by the two axis, we can get its uv coordinates
    // by mapping onto each of the axis
    float3 ProjectedPosition = ProjectPositionOntoVoronoiPlane(VoronoiVertex, Position);
    float u = GetDistanceAlongLine(VoronoiVertex, VoronoiVertex + XAxis, ProjectedPosition);
    float v = GetDistanceAlongLine(VoronoiVertex, VoronoiVertex + YAxis, ProjectedPosition);
    return float2(u, v);
}

#endif // INCLUDE_PAINTERLY