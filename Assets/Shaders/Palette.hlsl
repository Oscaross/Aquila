#ifndef AQUILA_PALETTE_INCLUDED
#define AQUILA_PALETTE_INCLUDED

// ----------------------------------------------------------------------------
// Shared colour discipline for every Aquila shader.
//
// The palette is uploaded by ShaderController from an AquilaPalette asset: the
// authored ramps, expanded with interpolated steps within each ramp so shader
// output has somewhere sensible to land without leaving the palette's colour
// space. Nothing here interpolates ACROSS ramps, so no muddy in-between hues.
//
// Dithering is offered, never imposed — shaders that already dither their own
// bands should use the hard-snap variants so the two patterns don't compound.
// ----------------------------------------------------------------------------

#define AQUILA_MAX_PALETTE 256

// Array length is fixed on first upload, so ShaderController always sends the
// full 256 and _PaletteCount bounds the meaningful entries.
float4 _PaletteColours[AQUILA_MAX_PALETTE];
float  _PaletteCount;
float  _PaletteAlphaSteps;

// Bias applied to the dither mix. Higher pulls harder toward the nearest
// swatch, so the crosshatch only appears where a colour is genuinely between
// two entries rather than spread evenly across flat regions.
float  _PaletteDitherBias;

// ---- Bayer ------------------------------------------------------------------
// One matrix and one indexing convention for the whole project, so every
// dithered effect lands on the same grid.

static const float AquilaBayer4[16] =
{
     0.0,  8.0,  2.0, 10.0,
    12.0,  4.0, 14.0,  6.0,
     3.0, 11.0,  1.0,  9.0,
    15.0,  7.0, 13.0,  5.0
};

/// gamePx must be in GAME pixels, not output pixels — screen-space for things
/// fixed to the camera, world-space for things fixed to the world.
float AquilaBayerThreshold(float2 gamePx)
{
    // floor before fmod so the index is integral; the offset keeps it positive
    // where gamePx goes negative off the left of the screen.
    int2 p = int2(fmod(floor(gamePx) + 4096.0, 4.0));
    return (AquilaBayer4[p.y * 4 + p.x] + 0.5) / 16.0;
}

// ---- Scalar quantisation ----------------------------------------------------

/// Snaps a 0-1 value to `steps` levels, dithering across each boundary.
float SnapDithered(float v, float steps, float threshold)
{
    float scaled = v * steps;
    return saturate((floor(scaled) + step(threshold, frac(scaled))) / steps);
}

/// Snaps alpha to a limited set of blend weights. Use where the layer must stay
/// translucent — the water plane, anything you need to see through.
float SnapAlpha(float a, float2 gamePx)
{
    return SnapDithered(a, _PaletteAlphaSteps, AquilaBayerThreshold(gamePx));
}

/// Turns translucency into hard on/off pixels: fully opaque or discarded.
/// Output stays exactly on-palette because nothing blends. Clips the fragment,
/// so call it early and never after work you still need.
void ClipDitheredAlpha(float a, float2 gamePx)
{
    clip(a - AquilaBayerThreshold(gamePx));
}

// ---- Palette snapping -------------------------------------------------------

/// Finds the two nearest palette entries by squared RGB distance.
/// Distances come back as squared; callers take the root only if they need it.
void AquilaFindNearestTwo(float3 rgb, out float3 best, out float3 second,
                          out float bestD, out float secondD)
{
    best = rgb; second = rgb;
    bestD = 1e9; secondD = 1e9;

    int count = (int)_PaletteCount;
    for (int i = 0; i < count; i++)
    {
        float3 p  = _PaletteColours[i].rgb;
        float3 d3 = rgb - p;
        float  d  = dot(d3, d3);

        if (d < bestD)
        {
            secondD = bestD; second = best;
            bestD   = d;     best   = p;
        }
        else if (d < secondD)
        {
            secondD = d; second = p;
        }
    }
}

TEXTURE3D(_PaletteLUT);
SAMPLER(sampler_PaletteLUT);

float3 SnapToPalette(float3 rgb)
{
    const float LUT_SIZE = 32;
    // Offset to cell centres — sampling the raw 0-1 range lands half a cell
    // outside at the extremes and clamps, biasing pure black and white.
    float3 uvw = saturate(rgb) * ((LUT_SIZE - 1.0) / LUT_SIZE) + (0.5 / LUT_SIZE);
    return SAMPLE_TEXTURE3D(_PaletteLUT, sampler_PaletteLUT, uvw).rgb;
}

#endif