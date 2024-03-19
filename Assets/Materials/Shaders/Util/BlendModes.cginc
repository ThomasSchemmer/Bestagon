#ifndef INCLUDE_BLENDMODES
#define INCLUDE_BLENDMODES

/**
 * Contains implementation for different blends ala photoshop.
 * See this article:
 * https://mouaif.wordpress.com/2009/01/05/photoshop-math-with-glsl-shaders/
 */

#define blendLinearDodge blendAdd

float blendAdd(float base, float blend)
{
    return min(base + blend, 1.0);
}

float3 blendAdd(float3 base, float3 blend)
{
    return min(base + blend, 1.0);
}

float3 blendAdd(float3 base, float3 blend, float opacity)
{
    return (blendAdd(base, blend) * opacity + base * (1.0 - opacity));
}

float blendLinearBurn(float base, float blend)
{
    return max(base + blend - 1.0, 0.0);
}

float3 blendLinearBurn(float3 base, float3 blend)
{
    return max(base + blend - 1.0, 0.0);
}

float3 blendLinearBurn(float3 base, float3 blend, float opacity)
{
    return (blendLinearBurn(base, blend) * opacity + base * (1.0 - opacity));
}

float blendLinearLight(float base, float blend)
{
    return blend < 0.5 ? blendLinearBurn(base, (2.0 * blend)) : blendLinearDodge(base, (2.0 * (blend - 0.5)));
}

float3 blendLinearLight(float3 base, float3 blend)
{
    return float3(blendLinearLight(base.r, blend.r), blendLinearLight(base.g, blend.g), blendLinearLight(base.b, blend.b));
}

float3 blendLinearLight(float3 base, float3 blend, float opacity)
{
    return (blendLinearLight(base, blend) * opacity + base * (1.0 - opacity));
}

#endif // INCLUDE_BLENDMODES