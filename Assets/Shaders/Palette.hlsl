#ifndef AQUILA_PALETTE_INCLUDED
#define AQUILA_PALETTE_INCLUDED

// ----------------------------------------------------------------------------
// Shared colour discipline for every Aquila shader.
//
// Lighting is an INDEX OFFSET, not a multiply. A pixel's authored colour is
// looked up to find which ramp it belongs to and where it sits in that ramp;
// the light level shifts it along that ramp; the result is read back out. The
// ramp never changes, so a dimmed brown lands on a darker brown rather than on
// whatever neutral happened to be nearest in RGB space.
//
// Uploaded by ShaderController:
//   _PaletteIndexLUT — 32^3 RGB cube. r = ramp, g = index in ramp, b = ramp length.
//   _PaletteRamps    — one row per ramp, one texel per entry, dark to light.
// ----------------------------------------------------------------------------

TEXTURE3D(_PaletteIndexLUT);
TEXTURE2D(_PaletteRamps);
SAMPLER(sampler_point_clamp);

float _PaletteMaxRampLength;
float _PaletteRampCount;
float _PaletteAlphaSteps;
int _DebugMode;
float _DebugRamp;
float _GlobalDarkness;

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

/// Snaps a 0-1 value to `steps` levels, dithering across each boundary.
float SnapDithered(float v, float steps, float threshold)
{
    float scaled = v * steps;
    return saturate((floor(scaled) + step(threshold, frac(scaled))) / steps);
}

/// Snaps alpha to a limited set of blend weights. For layers that must stay
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

/// The LUT is addressed in sRGB, where dark colours stay well separated.
/// Sprite textures are imported with sRGB ticked, so Unity converts them to
/// linear on sample; this converts back so the lookup sees the authored value.
float3 AquilaToSrgb(float3 c)
{
    return (c <= 0.0031308) ? c * 12.92 : 1.055 * pow(max(c, 0.0), 1.0 / 2.4) - 0.055;
}

void AquilaLookup(float3 rgb, out float ramp, out float index, out float rampLength)
{
    const float LUT_SIZE = 64;

    float3 srgb = saturate(AquilaToSrgb(rgb));
    float3 uvw = srgb * ((LUT_SIZE - 1.0) / LUT_SIZE) + (0.5 / LUT_SIZE);
    float4 hit = SAMPLE_TEXTURE3D(_PaletteIndexLUT, sampler_point_clamp, uvw);

    ramp       = floor(hit.r * 255.0 + 0.5);
    index      = floor(hit.g * 255.0 + 0.5);
    rampLength = floor(hit.b * 255.0 + 0.5);
}

/// Reads a colour back out of the ramp texture at a given position.
float3 AquilaReadRamp(float ramp, float index)
{
    float2 uv = float2((index + 0.5) / _PaletteMaxRampLength,
                       (ramp  + 0.5) / _PaletteRampCount);
    return SAMPLE_TEXTURE2D(_PaletteRamps, sampler_point_clamp, uv).rgb;
}

float3 HueFromID(float id)
{
    // Distinct hues per ramp, cycling. Not pretty, but adjacent ramps
    // are visibly different rather than a smooth red gradient.
    float h = frac(id * 0.618);          // golden ratio spreads hues evenly
    float3 k = float3(3.0, 2.0, 1.0) / 3.0;
    float3 p = abs(frac(h + k) * 6.0 - 3.0);
    return saturate(p - 1.0);
}

/// Snaps a colour to the palette without changing its brightness. For shaders
/// that only need the output on-palette — generated colours like the water
/// bands or the sun glimmer.
float3 SnapToPalette(float3 rgb)
{
    float ramp, index, rampLength;
    AquilaLookup(rgb, ramp, index, rampLength);
    return AquilaReadRamp(ramp, index);
}

/// Takes a pixel colour and darkness, figures out which ramp it belongs to, then shifts it proportionally up or down the ramp depending on it's darkness level. 
float3 LightWithPaletteHard(float3 rgb, float darkness)
{
    float ramp, index, rampLength;
    AquilaLookup(rgb, ramp, index, rampLength);
    
    float offset = round(darkness * (rampLength - 1.0));
    float shifted = clamp(index - offset, 0.0, rampLength - 1.0); // to land somewhere on the ramp we need to be between light index 0 and rampLength - 1
    
    if (_DebugMode == 1) return HueFromID(ramp);
    if (_DebugMode == 2) return HueFromID(shifted);
    if (_DebugMode == 3) return HueFromID(ramp * 16.0 + shifted);
    if (_DebugMode == 4) return (ramp == _DebugRamp) ? HueFromID(shifted) : 0.15;
    if (_DebugMode == 100) return float3(ramp, shifted, rampLength) / 255.0;
    return AquilaReadRamp(ramp, shifted);
    
    return AquilaReadRamp(ramp, shifted);
}

/// The common case: light by the current global level.
float3 LightWithPaletteGlobal(float3 rgb)
{
    return LightWithPaletteHard(rgb, _GlobalDarkness);
}

#endif