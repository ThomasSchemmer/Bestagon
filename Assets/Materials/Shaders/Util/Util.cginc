#ifndef INCLUDE_UTIL
#define INCLUDE_UTIL

// shadertoy 4djSRW
float2 hash22(float2 p){
	float3 p3 = frac(float3(p.xyx) * float3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yzx + 33.33);
    return frac((p3.xx + p3.yz) * p3.zy);
}
            
float map (float value, float from1, float to1, float from2, float to2) {
    return (value - from1) / round(to1 - from1) * (to2 - from2) + from2;
}

float3 map (float3 value, float3 from1, float3 to1, float3 from2, float3 to2){
    return float3(
        map(value.x, from1.x, to1.x, from2.x, to2.x),
        map(value.y, from1.y, to1.y, from2.y, to2.y),
        map(value.z, from1.z, to1.z, from2.z, to2.z)
    );
}

int round(float a){
    return floor(a + 0.5);
}



#define NOISE_SIMPLEX_1_DIV_289 0.00346020761245674740484429065744f
 
float mod289(float x)
{
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}
 
float2 mod289(float2 x)
{
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}
 
float3 mod289(float3 x)
{
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}
 
float4 mod289(float4 x)
{
    return x - floor(x * NOISE_SIMPLEX_1_DIV_289) * 289.0;
}


 
float4 grad4(float j, float4 ip)
{
    const float4 ones = float4(1.0, 1.0, 1.0, -1.0);
    float4 p, s;
    p.xyz = floor( frac(j * ip.xyz) * 7.0) * ip.z - 1.0;
    p.w = 1.5 - dot( abs(p.xyz), ones.xyz );
 
    p.xyz -= sign(p.xyz) * (p.w < 0);
 
    return p;
}


float taylorInvSqrt(float r) {
    return 1.79284291400159 - 0.85373472095314 * r;
}
 
float4 taylorInvSqrt(float4 r) {
    return 1.79284291400159 - 0.85373472095314 * r;
}

// ----------------------------
// see https://forum.unity.com/threads/2d-3d-4d-optimised-perlin-noise-cg-hlsl-library-cginc.218372/
float permute(float x) {
    return fmod(
        x*x*34.0 + x,
        289.0
    );
}

float3 permute(float3 x) {
    return fmod(
        x*x*34.0 + x,
        289.0
    );
}

float4 permute(float4 x)
{
    return mod289(
        x * x * 34.0 + x
    );
}
 

float snoise(float2 v)
{
    const float4 C = float4(
        0.211324865405187, // (3.0-sqrt(3.0))/6.0
        0.366025403784439, // 0.5*(sqrt(3.0)-1.0)
     -0.577350269189626, // -1.0 + 2.0 * C.x
        0.024390243902439  // 1.0 / 41.0
    );
 
// First corner
    float2 i = floor( v + dot(v, C.yy) );
    float2 x0 = v - i + dot(i, C.xx);
 
// Other corners 
    float4 x12 = x0.xyxy + C.xxzz;
    int2 i1 = (x0.x > x0.y) ? float2(1.0, 0.0) : float2(0.0, 1.0);
    x12.xy -= i1;
 
// Permutations
    i = fmod(i,289.0); // Avoid truncation effects in permutation
    float3 p = permute(
        permute(
                i.y + float3(0.0, i1.y, 1.0 )
        ) + i.x + float3(0.0, i1.x, 1.0 )
    );
 
    float3 m = max(
        0.5 - float3(
            dot(x0, x0),
            dot(x12.xy, x12.xy),
            dot(x12.zw, x12.zw)
        ),
        0.0
    );
    m = m*m ;
    m = m*m ;
 
// Gradients: 41 points uniformly over a line, mapped onto a diamond.
// The ring size 17*17 = 289 is close to a multiple of 41 (41*7 = 287)
 
    float3 x = 2.0 * frac(p * C.www) - 1.0;
    float3 h = abs(x) - 0.5;
    float3 ox = floor(x + 0.5);
    float3 a0 = x - ox;
 
// Normalise gradients implicitly by scaling m
// Approximation of: m *= inversesqrt( a0*a0 + h*h );
    m *= 1.79284291400159 - 0.85373472095314 * ( a0*a0 + h*h );
 
// Compute final noise value at P
    float3 g;
    g.x = a0.x * x0.x + h.x * x0.y;
    g.yz = a0.yz * x12.xz + h.yz * x12.yw;
    return 130.0 * dot(m, g);
}

float hash1( uint n ) 
{
    // integer hash copied from Hugo Elias
	n = (n << 13U) ^ n;
    n = n * (n * n * 15731U + 789221U) + 1376312589U;
    return float( n & uint(0x7fffffffU))/float(0x7fffffff);
}
// ----------------------------

// ----------------------------------- 3D -------------------------------------
float snoise(float3 v)
{
    const float2 C = float2(
        0.166666666666666667, // 1/6
        0.333333333333333333 // 1/3
    );
    const float4 D = float4(0.0, 0.5, 1.0, 2.0);
// First corner
    float3 i = floor(v + dot(v, C.yyy));
    float3 x0 = v - i + dot(i, C.xxx);
// Other corners
    float3 g = step(x0.yzx, x0.xyz);
    float3 l = 1 - g;
    float3 i1 = min(g.xyz, l.zxy);
    float3 i2 = max(g.xyz, l.zxy);
    float3 x1 = x0 - i1 + C.xxx;
    float3 x2 = x0 - i2 + C.yyy; // 2.0*C.x = 1/3 = C.y
    float3 x3 = x0 - D.yyy; // -1.0+3.0*C.x = -0.5 = -D.y
// Permutations
    i = mod289(i);
    float4 p = permute(
        permute(
            permute(
                    i.z + float4(0.0, i1.z, i2.z, 1.0)
            ) + i.y + float4(0.0, i1.y, i2.y, 1.0)
        ) + i.x + float4(0.0, i1.x, i2.x, 1.0)
    );
// Gradients: 7x7 points over a square, mapped onto an octahedron.
// The ring size 17*17 = 289 is close to a multiple of 49 (49*6 = 294)
    float n_ = 0.142857142857; // 1/7
    float3 ns = n_ * D.wyz - D.xzx;
    float4 j = p - 49.0 * floor(p * ns.z * ns.z); // mod(p,7*7)
    float4 x_ = floor(j * ns.z);
    float4 y_ = floor(j - 7.0 * x_); // mod(j,N)
    float4 x = x_ * ns.x + ns.yyyy;
    float4 y = y_ * ns.x + ns.yyyy;
    float4 h = 1.0 - abs(x) - abs(y);
    float4 b0 = float4(x.xy, y.xy);
    float4 b1 = float4(x.zw, y.zw);
    //float4 s0 = float4(lessThan(b0,0.0))*2.0 - 1.0;
    //float4 s1 = float4(lessThan(b1,0.0))*2.0 - 1.0;
    float4 s0 = floor(b0) * 2.0 + 1.0;
    float4 s1 = floor(b1) * 2.0 + 1.0;
    float4 sh = -step(h, float4(0, 0, 0, 0));
    float4 a0 = b0.xzyw + s0.xzyw * sh.xxyy;
    float4 a1 = b1.xzyw + s1.xzyw * sh.zzww;
    float3 p0 = float3(a0.xy, h.x);
    float3 p1 = float3(a0.zw, h.y);
    float3 p2 = float3(a1.xy, h.z);
    float3 p3 = float3(a1.zw, h.w);
//Normalise gradients
    float4 norm = rsqrt(float4(dot(p0, p0), dot(p1, p1), dot(p2, p2), dot(p3, p3)));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;
// Mix final noise value
    float4 m = max(0.5 - float4(dot(x0, x0), dot(x1, x1), dot(x2, x2), dot(x3, x3)), 0.0);
    m = m * m;
    return 105.0 * dot(m * m, float4(dot(p0, x0), dot(p1, x1), dot(p2, x2), dot(p3, x3)));
}



/* --------------------4D simplex noise------------------------- */

float snoise(float4 v)
{
    const float4 C = float4(
        0.138196601125011, // (5 - sqrt(5))/20 G4
        0.276393202250021, // 2 * G4
        0.414589803375032, // 3 * G4
     -0.447213595499958  // -1 + 4 * G4
    );
 
// First corner
    float4 i = floor(
        v +
        dot(
            v,
            0.309016994374947451 // (sqrt(5) - 1) / 4
        )
    );
    float4 x0 = v - i + dot(i, C.xxxx);
 
// Other corners
 
// Rank sorting originally contributed by Bill Licea-Kane, AMD (formerly ATI)
    float4 i0;
    float3 isX = step( x0.yzw, x0.xxx );
    float3 isYZ = step( x0.zww, x0.yyz );
    i0.x = isX.x + isX.y + isX.z;
    i0.yzw = 1.0 - isX;
    i0.y += isYZ.x + isYZ.y;
    i0.zw += 1.0 - isYZ.xy;
    i0.z += isYZ.z;
    i0.w += 1.0 - isYZ.z;
 
    // i0 now contains the unique values 0,1,2,3 in each channel
    float4 i3 = saturate(i0);
    float4 i2 = saturate(i0-1.0);
    float4 i1 = saturate(i0-2.0);
 
    //    x0 = x0 - 0.0 + 0.0 * C.xxxx
    //    x1 = x0 - i1  + 1.0 * C.xxxx
    //    x2 = x0 - i2  + 2.0 * C.xxxx
    //    x3 = x0 - i3  + 3.0 * C.xxxx
    //    x4 = x0 - 1.0 + 4.0 * C.xxxx
    float4 x1 = x0 - i1 + C.xxxx;
    float4 x2 = x0 - i2 + C.yyyy;
    float4 x3 = x0 - i3 + C.zzzz;
    float4 x4 = x0 + C.wwww;
 
// Permutations
    i = fmod(i,289.0);
    float j0 = permute(
        permute(
            permute(
                permute(i.w) + i.z
            ) + i.y
        ) + i.x
    );
    float4 j1 = permute(
        permute(
            permute(
                permute (
                    i.w + float4(i1.w, i2.w, i3.w, 1.0 )
                ) + i.z + float4(i1.z, i2.z, i3.z, 1.0 )
            ) + i.y + float4(i1.y, i2.y, i3.y, 1.0 )
        ) + i.x + float4(i1.x, i2.x, i3.x, 1.0 )
    );
 
// Gradients: 7x7x6 points over a cube, mapped onto a 4-cross polytope
// 7*7*6 = 294, which is close to the ring size 17*17 = 289.
    const float4 ip = float4(
        0.003401360544217687075, // 1/294
        0.020408163265306122449, // 1/49
        0.142857142857142857143, // 1/7
        0.0
    );
 
    float4 p0 = grad4(j0, ip);
    float4 p1 = grad4(j1.x, ip);
    float4 p2 = grad4(j1.y, ip);
    float4 p3 = grad4(j1.z, ip);
    float4 p4 = grad4(j1.w, ip);
 
// Normalise gradients
    float4 norm = taylorInvSqrt(float4(
        dot(p0, p0),
        dot(p1, p1),
        dot(p2, p2),
        dot(p3, p3)
    ));
    p0 *= norm.x;
    p1 *= norm.y;
    p2 *= norm.z;
    p3 *= norm.w;
    p4 *= taylorInvSqrt( dot(p4, p4) );
 
// Mix contributions from the five corners
    float3 m0 = max(
        0.6 - float3(
            dot(x0, x0),
            dot(x1, x1),
            dot(x2, x2)
        ),
        0.0
    );
    float2 m1 = max(
        0.6 - float2(
            dot(x3, x3),
            dot(x4, x4)
        ),
        0.0
    );
    m0 = m0 * m0;
    m1 = m1 * m1;
 
    return 49.0 * (
        dot(
            m0*m0,
            float3(
                dot(p0, x0),
                dot(p1, x1),
                dot(p2, x2)
            )
        ) + dot(
            m1*m1,
            float2(
                dot(p3, x3),
                dot(p4, x4)
            )
        )
    );
}
 

/* --------------------stacked simplex noise------------------------- */

float ssnoise(float3 uv, float Scale, float Offset, float Iterations, float Factor)
{
    float Result = snoise(uv * Scale + Offset);
    
    for (int i = 1; i < Iterations; i++)
    {
        Factor *= 0.5;
        Scale *= 2;
        float Noise = snoise(uv * Scale + Offset) * Factor;
        // old range is already from 0..1, if we add another 0..Factor its too much again
        // Simply add both results proportionally
        float Proportion = pow(2, i);
        Result = (Result + Noise) * (Proportion / (Proportion + 1));
    }
    return Result;
}

/* -------------------- VORONOI ------------------------- */

float voronoi(float2 position)
{
    float2 base = floor(position);
    float minDistanceSqr = 10000;
                [unroll]
    for (int x = -1; x <= 1; x++)
    {
                    [unroll]   
        for (int y = -1; y <= 1; y++)
        {
            float2 cell = base + float2(x, y);
            float2 posInCell = cell + hash22(cell);
            float2 diff = posInCell - position;
            float distanceSqr = diff.x * diff.x + diff.y * diff.y;
            if (distanceSqr < minDistanceSqr)
            {
                minDistanceSqr = distanceSqr;
            }
        }
    }
    return minDistanceSqr;
}

//https://www.shadertoy.com/view/4djSRW old hash had sine, with was unstable
float3 hash3(float3 p3)
{
    p3 = frac(p3 * float3(.1031, .1030, .0973));
    p3 += dot(p3, p3.yxz + 33.33);
    return frac((p3.xxy + p3.yxx) * p3.zyx);
}

// https://www.shadertoy.com/view/ldl3Dl 
float3 voronoi3(float3 Position)
{
    float3 PositionOfCell = floor(Position);

    float ClosestID = 0.0;
    float ClosestDistanceSqr = 100;
    float3 ClosestVertexPosition = float3(100, 100, 100);
    
    for (int k = -1; k <= 1; k++)
        for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                float3 OffsetOfCell = float3(float(i), float(j), float(k));
                
                // avoid passing in 0 
                float3 OffsetInCell = hash3(PositionOfCell + OffsetOfCell + float3(0, 0.1, 0));
                float3 VertexPosition = PositionOfCell + OffsetOfCell + OffsetInCell;
                float3 Distance = Position - VertexPosition;
                float DistanceSqr = dot(Distance, Distance);

                if (DistanceSqr < ClosestDistanceSqr)
                {
                    ClosestVertexPosition = VertexPosition;
                    ClosestID = dot(PositionOfCell + OffsetOfCell, float3(5.0, 57.0, 113.0));
                    ClosestDistanceSqr = DistanceSqr;
                }
            }
    
    return ClosestVertexPosition;
}

// does not tile correctly!
float voronoi3Tiled(float3 Position, float CellCount)
{
    float3 PositionCenter = floor(Position);

    float ClosestDistanceSqr = CellCount * CellCount;
    
    for (int k = -1; k <= 1; k++)
        for (int j = -1; j <= 1; j++)
            for (int i = -1; i <= 1; i++)
            {
                float3 CellOffset = float3(float(i), float(j), float(k));
                float3 CellPosition = PositionCenter + CellOffset;
                float3 WrappedCellPosition = (CellPosition + CellCount) % CellCount;
                
                // avoid passing in 0 
                float3 OffsetInCell = hash3(WrappedCellPosition + float3(0, 0.1, 0));
                float3 VertexPosition = WrappedCellPosition + OffsetInCell;
                float3 Distance = (Position - VertexPosition) - (CellPosition - WrappedCellPosition);
                float DistanceSqr = dot(Distance, Distance);

                if (DistanceSqr < ClosestDistanceSqr)
                {
                    ClosestDistanceSqr = DistanceSqr;
                }
            }

    
    return ClosestDistanceSqr;
}

float svoronoi3Tiled(float3 Position, float CellCount, float Iterations, float Factor)
{
    float Result = voronoi3Tiled(Position, CellCount);
    float Scale = 1;
    
    for (int i = 1; i < Iterations; i++)
    {
        Factor *= 0.5;
        Scale *= 2;
        float Noise = voronoi3Tiled(Position * Scale, CellCount) * Factor;
        // old range is already from 0..1, if we add another 0..Factor its too much again
        // Simply add both results proportionally
        float Proportion = pow(2, i);
        Result = (Result + Noise) * (Proportion / (Proportion + 1));
    }
    return Result;
}

/** Draws a line between each two voronoi cells */
void LineVoronoi(float2 Position, out float DistanceDiff, out int TargetCell)
{
    float2 base = floor(Position);
    float FirstDistanceSqr = 10000;
    float SecondDistanceSqr = 10000;
                [unroll]
    for (int x = -1; x <= 1; x++)
    {
                    [unroll]   
        for (int y = -1; y <= 1; y++)
        {
            float2 cell = base + float2(x, y);
            float2 posInCell = cell + hash22(cell);
            float2 diff = posInCell - Position;
            float distanceSqr = diff.x * diff.x + diff.y * diff.y;
            if (distanceSqr < FirstDistanceSqr)
            {
                if (FirstDistanceSqr < SecondDistanceSqr)
                {
                    SecondDistanceSqr = FirstDistanceSqr;
                }
                FirstDistanceSqr = distanceSqr;
                TargetCell = (y + 1) * 3 + (x + 1);
            }
        }
    }
    DistanceDiff = abs(FirstDistanceSqr - SecondDistanceSqr);
}


#endif // INCLUDE_UTIL
            