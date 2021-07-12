﻿#ifndef __bcc_noise_8_hlsl_
#define __bcc_noise_8_hlsl_

////////////////// K.jpg's Smooth Re-oriented 8-Point BCC Noise //////////////////
//////////////////// Output: float4(dF/dx, dF/dy, dF/dz, value) ////////////////////

// Borrowed from Stefan Gustavson's noise code
float4 permute(float4 t)
{
    return t * (t * 34.0 + 133.0);
}

float mod(float x, float y)
{
    return x - y * floor(x / y);
}

float2 mod(float2 x, float2 y)
{
    return x - y * floor(x / y);
}

float3 mod(float3 x, float3 y)
{
    return x - y * floor(x / y);
}

float4 mod(float4 x, float4 y)
{
    return x - y * floor(x / y);
}

// Gradient set is a normalized expanded rhombic dodecahedron
float3 grad(float hash)
{
    // Random vertex of a cube, +/- 1 each
    float3 cube = mod(floor(hash / float3(1.0, 2.0, 4.0)), 2.0) * 2.0 - 1.0;

    // Random edge of the three edges connected to that vertex
    // Also a cuboctahedral vertex
    // And corresponds to the face of its dual, the rhombic dodecahedron
    float3 cuboct = cube;

    int index = int(hash / 16.0);
    if (index == 0)
        cuboct.x = 0.0;
    else if (index == 1)
        cuboct.y = 0.0;
    else
        cuboct.z = 0.0;

    // In a funky way, pick one of the four points on the rhombic face
    float type = mod(floor(hash / 8.0), 2.0);
    float3 rhomb = (1.0 - type) * cube + type * (cuboct + cross(cube, cuboct));

    // Expand it so that the new edges are the same length
    // as the existing ones
    float3 grad = cuboct * 1.22474487139 + rhomb;

    // To make all gradients the same length, we only need to shorten the
    // second type of vector. We also put in the whole noise scale constant.
    // The compiler should reduce it into the existing floats. I think.
    grad *= (1.0 - 0.042942436724648037 * type) * 3.5946317686139184;

    return grad;
}

// BCC lattice split up into 2 cube lattices
float4 bccNoiseDerivativesPart(float3 X)
{
    float3 b = floor(X);
    float4 i4 = float4(X - b, 2.5);

    // Pick between each pair of oppposite corners in the cube.
    float3 v1 = b + floor(dot(i4, float4(.25, .25, .25, .25)));
    float3 v2 = b + float3(1, 0, 0) + float3(-1, 1, 1) * floor(dot(i4, float4(-.25, .25, .25, .35)));
    float3 v3 = b + float3(0, 1, 0) + float3(1, -1, 1) * floor(dot(i4, float4(.25, -.25, .25, .35)));
    float3 v4 = b + float3(0, 0, 1) + float3(1, 1, -1) * floor(dot(i4, float4(.25, .25, -.25, .35)));

    // Gradient hashes for the four vertices in this half-lattice.
    float4 hashes = permute(mod(float4(v1.x, v2.x, v3.x, v4.x), 289.0));
    hashes = permute(mod(hashes + float4(v1.y, v2.y, v3.y, v4.y), 289.0));
    hashes = mod(permute(mod(hashes + float4(v1.z, v2.z, v3.z, v4.z), 289.0)), 48.0);

    // Gradient extrapolations & kernel function
    float3 d1 = X - v1;
    float3 d2 = X - v2;
    float3 d3 = X - v3;
    float3 d4 = X - v4;
    float4 a = max(0.75 - float4(dot(d1, d1), dot(d2, d2), dot(d3, d3), dot(d4, d4)), 0.0);
    float4 aa = a * a;
    float4 aaaa = aa * aa;
    float3 g1 = grad(hashes.x);
    float3 g2 = grad(hashes.y);
    float3 g3 = grad(hashes.z);
    float3 g4 = grad(hashes.w);
    float4 extrapolations = float4(dot(d1, g1), dot(d2, g2), dot(d3, g3), dot(d4, g4));

    float3x4 derivativeMatrix = {d1, d2, d3, d4};
    float3x4 gradiantMatrix = {g1, g2, g3, g4};

    // Derivatives of the noise
    float3 derivative = -8.0 * mul(derivativeMatrix, (aa * a * extrapolations))
        + mul(gradiantMatrix, aaaa);

    // Return it all as a float4
    return float4(derivative, dot(aaaa, extrapolations));
}

// Rotates domain, but preserve shape. Hides grid better in cardinal slices.
// Good for texturing 3D objects with lots of flat parts along cardinal planes.
float4 bccNoiseDerivatives_XYZ(float3 X)
{
    X = dot(X, float3(2.0 / 3.0, 2.0 / 3.0, 2.0 / 3.0)) - X;

    float4 result = bccNoiseDerivativesPart(X) + bccNoiseDerivativesPart(X + 144.5);

    return float4(dot(result.xyz, float3(2.0 / 3.0, 2.0 / 3.0, 2.0 / 3.0)) - result.xyz, result.w);
}

// Gives X and Y a triangular alignment, and lets Z move up the main diagonal.
// Might be good for terrain, or a time varying X/Y plane. Z repeats.
float4 bccNoiseDerivatives_PlaneFirst(float3 X)
{
    // Not a skew transform.
    float3x3 orthonormalMap = {
        0.788675134594813, -0.211324865405187, -0.577350269189626,
        -0.211324865405187, 0.788675134594813, -0.577350269189626,
        0.577350269189626, 0.577350269189626, 0.577350269189626
    };

    X = mul(orthonormalMap, X);
    float4 result = bccNoiseDerivativesPart(X) + bccNoiseDerivativesPart(X + 144.5);

    return float4(mul(result.xyz, orthonormalMap), result.w);
}

//////////////////////////////// End noise code ////////////////////////////////

#endif
