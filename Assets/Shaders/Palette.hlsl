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

TEXTURE3D(_PaletteIndexLUT);
TEXTURE2D(_PaletteRamps);
SAMPLER(sampler_point_clamp);

#define PIXELS_PER_UNIT 16.0

// CORE
float _PaletteMaxRampLength; // computed by the ShaderController and is just the number of entries in the longest ramp to prevent index overflows
float _PaletteRampCount; // the number of unique ramps loaded from the engine's palette
float _PaletteAlphaSteps; // how many discrete alpha steps from 0..255 we are allowed to take
float _PaletteBlockCount; // how many unique palettes for coloured light there are, for example, neutral lit, cool lit, warm lit
float _GlobalLightTemperatureBlock; // what temperature preset the global light colour is. Warm during sunset/sunrise, cool during nighttime, neutral at the day etc...
float _GlobalDarkness; // what is the current global brightness level published by the SkyController system?

// LOCAL LIGHTING
#define AQUILA_MAX_LIGHTS 32

float4 _LightData[AQUILA_MAX_LIGHTS]; // contains the x = pos.x, y = pos.y, z = radius and w = intensity
float4 _LightMeta[AQUILA_MAX_LIGHTS]; // auxilliary data about how the light source behaves. x = temperature push (i.e. strong warm light or weak warm light)
int _LightCount; // how many lights we are publishing data for

// DEBUG
int _DebugMode; // the type of debug view we want to see i.e. hue grouped by ramps, index, ramp and index, etc...
float _DebugRamp; // if we are looking at behaviour of a singular ramp, which one should be shown?

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

/// Returns the on-palette colour for this ramp, at the correct index on the correct block palette.
/// A block palette is the type of light colour, e.g. neutral light (sunlit) or mild warm light (torch/fire/sunset/sunrise lighting).
float3 AquilaReadRamp(float ramp, float index, float block)
{
    float row = block * _PaletteRampCount + ramp;
    float2 uv = float2((index + 0.5) / _PaletteMaxRampLength,
                       (row + 0.5) / (_PaletteRampCount * _PaletteBlockCount));
    return SAMPLE_TEXTURE2D(_PaletteRamps, sampler_point_clamp, uv).rgb;
}

/// Returns the on-palette colour for this ramp, at the correct index.
/// e.g. if we are on the wood ramp at idx = 0, this returns the darkest wood colour on the current palette.
float3 AquilaReadRamp(float ramp, float index)
{
    return AquilaReadRamp(ramp, index, _GlobalLightTemperatureBlock);
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

/// Debug visualisations, shared by every lighting path so switching a shader
/// between global and local lighting doesn't silently lose the debug views.
/// Returns true when a mode is active; callers return `result` immediately.
bool AquilaDebugOverride(float ramp, float shifted, float rampLength,
                         float brightness, float block, out float3 result)
{
    result = 0.0;
    if (_DebugMode == 0) return false;

    if (_DebugMode == 1)   result = HueFromID(ramp);                        // ramp identity
    if (_DebugMode == 2)   result = HueFromID(shifted);                     // index within ramp
    if (_DebugMode == 3)   result = HueFromID(ramp * 16.0 + shifted);       // both combined
    if (_DebugMode == 4)   result = (ramp == _DebugRamp) ? HueFromID(shifted) : 0.15;
    if (_DebugMode == 5)   result = saturate(brightness);                   // local light field
    if (_DebugMode == 6)   result = HueFromID(block);                       // resolved block
    if (_DebugMode == 100) result = float3(ramp, shifted, rampLength) / 255.0;

    return true;
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
/// IGNORES LOCAL LIGHT SOURCES - OBJECTS THAT USE THIS PATH WILL NOT BE ILLUMINATED BY LOCAL LIGHT SOURCES.
float3 LightWithPaletteGlobally(float3 rgb, float darkness)
{
    float ramp, index, rampLength;
    AquilaLookup(rgb, ramp, index, rampLength);

    float offset  = round(darkness * (rampLength - 1.0));
    float shifted = clamp(index - offset, 0.0, rampLength - 1.0);

    float3 dbg;
    if (AquilaDebugOverride(ramp, shifted, rampLength,
                            0.0, _GlobalLightTemperatureBlock, dbg)) return dbg;

    return AquilaReadRamp(ramp, shifted);
}

/// Shifts a colour along its ramp by an explicit number of steps. Positive steps move to the light end, negative to the dark end. 
float3 ShiftPaletteSteps(float3 rgb, float steps)
{
    float ramp, index, rampLength;
    AquilaLookup(rgb, ramp, index, rampLength);
    
    float shifted = clamp(index + steps, 0.0, rampLength - 1.0);
    return AquilaReadRamp(ramp, shifted);
}

/// Accumulates every local light reaching this world position.
/// brightness adds to the lit level; temperature pushes the block selection warmer.
/// Bounds temperature by the strongest light's temperature, rather than additive increase beyond the maximum temperature
void AquilaAccumulateLights(float2 worldPos, out float brightness, out float temperature)
{
    brightness = 0.0;
    float tempWeighted = 0.0;
    float totalWeight = 0.0;

    for (int i = 0; i < _LightCount; i++)
    {
        float2 delta = worldPos - _LightData[i].xy;
        float radius = max(_LightData[i].z, 1e-4);

        float atten = saturate(1.0 - dot(delta, delta) / (radius * radius));
        atten = atten * atten * _LightData[i].w;

        brightness   += atten;
        tempWeighted += atten * _LightMeta[i].x;
        totalWeight  += atten;
    }

    temperature = tempWeighted / max(1.0, totalWeight);
}

/// Before lighting, snap screen fragments to a quantised pixel position so we still get crisp, pixel perfect lighting.
float2 SnapToGrid(float2 worldPos)
{
    return (floor(worldPos * PIXELS_PER_UNIT) + 0.5) / PIXELS_PER_UNIT;
}

float AquilaHash(int2 cell)
{
    float2 p = frac(float2(cell) * float2(0.1031, 0.1030));
    p += dot(p, p.yx + 33.33);
    return frac((p.x + p.y) * p.x);
}

/// The full local-lighting path: global darkness as the floor, lights on top,
/// one ramp lookup and one block decision at the end.
float3 LightWithPaletteLocal(float3 rgb, float2 worldPos)
{
    worldPos = SnapToGrid(worldPos); // quantise pixel positions so we don't get internal pixel fragments being illuminated differently
    
    float ramp, index, rampLength;
    AquilaLookup(rgb, ramp, index, rampLength);

    float brightness, temperature;
    AquilaAccumulateLights(worldPos, brightness, temperature);

    int2 cell = int2(floor(worldPos * PIXELS_PER_UNIT));
    
    // Dither index is the light intensity offset. We connect this to the time elapsed so that all pixels brighten and darken at psuedorandom times to give the flicker effect.
    int tick = (int)floor(_Time.y * 4.0); // 4 discrete ticks for 4 discrete states for each pixel as time varies
    
    float churn = saturate(brightness * 2.0); // a pixel that isn't illuminated at all by a local light source should not have the dither effect applied to it => clamps to 0 for brightness = 0
    // If churn is 0 we land on float a which is the static dithered value of the cell, which just matches its light level. Otherwise, we lerp toward the light-offset shimmer.
    float ditherIndex = lerp(AquilaHash(cell), 
        AquilaHash(cell + int2(tick * 37, tick * 101)),
        churn);
    
    
    float ditherBlock = AquilaHash(cell + int2(17, 31));
    
    float darkness = saturate(_GlobalDarkness - brightness);
    
    // Whatever block this pixel is on, plus the additional temperature that the light source is emitting
    float blockRaw = _GlobalLightTemperatureBlock + temperature;
    float blockBase = floor(blockRaw);
    float block = clamp(blockBase + step(ditherBlock, blockRaw - blockBase),
                            0.0, _PaletteBlockCount - 1.0);
    
    float offset  = floor(darkness * (rampLength - 1.0) + ditherIndex);
    float shifted = clamp(index - offset, 0.0, rampLength - 1.0);

    float3 dbg;
    if (AquilaDebugOverride(ramp, shifted, rampLength, brightness, block, dbg)) return dbg;

    return AquilaReadRamp(ramp, shifted, block);
}

#endif