using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

/// <summary>
/// Owns the project's colour discipline.
///
/// Bakes two lookup textures from the authored palette:
///
///   _PaletteIndexLUT — a 32x32x32 RGB cube. Each cell holds the RAMP and INDEX of the
///                      nearest palette entry rather than the colour itself, so shaders
///                      can move a pixel ALONG its own ramp instead of searching the
///                      whole palette for a lit result. That's what stops a dimmed brown
///                      landing on a neutral dark or a hazed grey picking up cyan.
///
///   _PaletteRamps    — a small 2D texture holding the ramps themselves. One row per
///                      ramp, one texel per entry. Later this grows downward to hold
///                      warm and cool rows of the same ramps.
///
/// Lighting is then an index offset: intensity moves a pixel down or up its ramp, and
/// colour temperature (once authored) selects which row block to read from. Nothing
/// multiplies, so nothing can produce a colour that isn't in the palette.
///
/// Both textures are rebuilt on enable and on demand, never per frame.
/// </summary>
[ExecuteAlways]
public class ShaderController : MonoBehaviour
{
    private const int LutSize = 64;   // cells per axis; n^3 entries

    public static ShaderController Instance { get; private set; }

    [SerializeField] private AquilaPalette palette;

    [Header("Quantisation")]
    [Tooltip("Discrete alpha levels available to shaders that blend rather than clip.")]
    [SerializeField, Range(2, 32)] private int alphaSteps = 16;

    public static int maxOffset = 5;

    private static readonly int IndexLutID    = Shader.PropertyToID("_PaletteIndexLUT");
    private static readonly int RampTexID     = Shader.PropertyToID("_PaletteRamps");
    private static readonly int MaxRampLenID  = Shader.PropertyToID("_PaletteMaxRampLength");
    private static readonly int RampCountID   = Shader.PropertyToID("_PaletteRampCount");
    private static readonly int AlphaStepsID  = Shader.PropertyToID("_PaletteAlphaSteps");

    private Texture3D indexLut;
    private Texture2D rampTexture;

    // ---- Quantisation ------------------------------------------------------------
    // Everything published to a shader goes through one of these, so the whole scene
    // steps at the same moments rather than each shader drifting across its own
    // boundaries independently.

    public static float QuantiseAlpha(float value) =>
        Quantise(value, Instance != null ? Instance.alphaSteps : 16);
    
    private static float Quantise(float value, int steps) =>
        steps <= 1 ? value : Mathf.Round(value * steps) / steps;

    // ---- Lifecycle ---------------------------------------------------------------

    private void OnEnable()
    {
        Instance = this;
        RebuildPalette();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        ReleaseTextures();
    }

    private void LateUpdate()
    {
        Shader.SetGlobalFloat(AlphaStepsID, alphaSteps);
    }

    /// <summary>
    /// Rebuilds both lookup textures from the palette asset. Call after editing the
    /// palette — OnValidate here won't catch changes made to the asset itself.
    /// </summary>
    [ContextMenu("Rebuild Palette")]
    public void RebuildPalette()
    {
        if (palette == null) return;

        List<Color[]> ramps = palette.GetRamps();
        if (ramps == null || ramps.Count == 0)
        {
            Debug.LogWarning($"{name}: palette produced no ramps — check the definitions.", this);
            return;
        }

        int maxRampLength = 0;
        foreach (Color[] ramp in ramps) maxRampLength = Mathf.Max(maxRampLength, ramp.Length);

        ReleaseTextures();

        rampTexture = BuildRampTexture(ramps, maxRampLength);
        indexLut    = BuildIndexLut(ramps);

        Shader.SetGlobalTexture(RampTexID, rampTexture);
        Shader.SetGlobalTexture(IndexLutID, indexLut);
        Shader.SetGlobalFloat(MaxRampLenID, maxRampLength);
        Shader.SetGlobalFloat(RampCountID, ramps.Count);

        Debug.Log($"{name}: baked {ramps.Count} ramps, longest {maxRampLength}.", this);
    }

    // ---- Ramp texture ------------------------------------------------------------

    /// <summary>
    /// One row per ramp, one texel per entry, left (darkest) to right (lightest).
    /// Short ramps repeat their last entry to fill the row — the shader clamps to the
    /// real length anyway, so the padding is never read, but it keeps the texture
    /// rectangular and avoids sampling undefined texels.
    /// </summary>
    private static Texture2D BuildRampTexture(List<Color[]> ramps, int maxRampLength)
    {
        var texture = new Texture2D(maxRampLength, ramps.Count,
            TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[maxRampLength * ramps.Count];

        for (int y = 0; y < ramps.Count; y++)
        {
            Color[] ramp = ramps[y];
            for (int x = 0; x < maxRampLength; x++)
                pixels[y * maxRampLength + x] = ramp[Mathf.Min(x, ramp.Length - 1)];
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    // ---- Index LUT ---------------------------------------------------------------

    /// <summary>
    /// For each cell of the RGB cube, finds the nearest palette entry and stores WHERE
    /// it is: ramp in red, index within that ramp in green, ramp length in blue. The
    /// length travels with the lookup so the shader can clamp an offset without needing
    /// a separate array of lengths.
    /// </summary>
    private static Texture3D BuildIndexLut(List<Color[]> ramps)
    {
        var texture = new Texture3D(LutSize, LutSize, LutSize,
            GraphicsFormat.R8G8B8A8_UNorm, TextureCreationFlags.None)
        {
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            hideFlags = HideFlags.HideAndDontSave
        };

        // Flatten to a lookup of (lab, ramp, index) so the inner loop is a simple scan.
        var labs   = new List<Vector3>();
        var coords = new List<Vector2Int>();
        var lengths = new List<int>();

        for (int y = 0; y < ramps.Count; y++)
        {
            for (int x = 0; x < ramps[y].Length; x++)
            {
                labs.Add(RgbToOklab(ToLinear(ramps[y][x])));
                coords.Add(new Vector2Int(y, x));
                lengths.Add(ramps[y].Length);
            }
        }

        var pixels = new Color32[LutSize * LutSize * LutSize];

        for (int b = 0; b < LutSize; b++)
        for (int g = 0; g < LutSize; g++)
        for (int r = 0; r < LutSize; r++)
        {
            Color input = new Color(r / (LutSize - 1f), g / (LutSize - 1f), b / (LutSize - 1f));
            Vector3 inputLab = RgbToOklab(ToLinear(input));

            float bestDistance = float.MaxValue;
            int best = 0;

            for (int i = 0; i < labs.Count; i++)
            {
                float distance = (labs[i] - inputLab).sqrMagnitude;
                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = i;
                }
            }

            Color exact = ramps[coords[best].x][coords[best].y]; 
            bool isExact = Mathf.Abs(exact.r - input.r) < 0.5f / (LutSize - 1)
                           && Mathf.Abs(exact.g - input.g) < 0.5f / (LutSize - 1)
                           && Mathf.Abs(exact.b - input.b) < 0.5f / (LutSize - 1);
            
            pixels[r + g * LutSize + b * LutSize * LutSize] = new Color32(
                (byte)coords[best].x,   // ramp
                (byte)coords[best].y,   // index within ramp
                (byte)lengths[best],    // that ramp's real length
                (byte)(isExact ? 255 : 0));
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }
    
        /// <summary>
    /// Dumps every ramp and the hex values the bake actually produced, then verifies each
    /// authored colour resolves back to its own ramp and index through the LUT. A colour
    /// that doesn't round-trip is one that shares a LUT cell with an entry from another
    /// ramp — it can never be lit correctly, and raising LutSize is the fix.
    /// </summary>
    [ContextMenu("Log Palette")]
    public void LogPalette()
    {
        if (palette == null) { Debug.LogWarning($"{name}: no palette assigned.", this); return; }
        
        List<Color[]> ramps = palette.GetRamps();
        if (ramps == null || ramps.Count == 0) { Debug.LogWarning($"{name}: no ramps.", this); return; }
        
        int total = 0;
        for (int y = 0; y < ramps.Count; y++)
        {
            var hex = new List<string>();
            foreach (Color c in ramps[y]) hex.Add(ColorUtility.ToHtmlStringRGB(c));
            total += ramps[y].Length;

            Debug.Log($"Ramp {y} ({ramps[y].Length}): {string.Join(" ", hex)}");
        }

        Debug.Log($"{ramps.Count} ramps, {total} colours total.");

        // ---- Round-trip check ----
        if (indexLut == null) { Debug.LogWarning("No LUT baked — run Rebuild Palette first."); return; }

        Color32[] cells = indexLut.GetPixels32();
        int collisions = 0;

        for (int y = 0; y < ramps.Count; y++)
        for (int x = 0; x < ramps[y].Length; x++)
        {
            Color c = ramps[y][x];
            int cr = Mathf.Clamp(Mathf.RoundToInt(c.r * (LutSize - 1)), 0, LutSize - 1);
            int cg = Mathf.Clamp(Mathf.RoundToInt(c.g * (LutSize - 1)), 0, LutSize - 1);
            int cb = Mathf.Clamp(Mathf.RoundToInt(c.b * (LutSize - 1)), 0, LutSize - 1);

            Color32 stored = cells[cr + cg * LutSize + cb * LutSize * LutSize];

            if (stored.r != y || stored.g != x)
            {
                collisions++;
                Debug.LogWarning(
                    $"#{ColorUtility.ToHtmlStringRGB(ramps[y][x])} (ramp {y}, index {x}) " +
                    $"resolves to ramp {stored.r}, index {stored.g} " +
                    $"— cell [{cr},{cg},{cb}] is shared.");
            }
        }

        Debug.Log(collisions == 0
            ? "All colours round-trip correctly."
            : $"{collisions} colour(s) collide in the LUT — raise LutSize to 64.");
    }
        
    [ContextMenu("Trace Dirt Colours")]
    public void TraceColours()
    {
        string[] hexes = { "33261B", "513C29", "6E5336", "8A6A46" };

        Color32[] cells = indexLut.GetPixels32();

        foreach (string h in hexes)
        {
            ColorUtility.TryParseHtmlString("#" + h, out Color c);

            int cr = Mathf.RoundToInt(c.r * (LutSize - 1));
            int cg = Mathf.RoundToInt(c.g * (LutSize - 1));
            int cb = Mathf.RoundToInt(c.b * (LutSize - 1));

            Color32 stored = cells[cr + cg * LutSize + cb * LutSize * LutSize];

            Debug.Log($"#{h} → cell [{cr},{cg},{cb}] → ramp {stored.r}, index {stored.g}, " +
                      $"len {stored.b}, exact {stored.a}");
        }
    }

    // ---- Colour space ------------------------------------------------------------
    // Linearise for the COMPARISON only. What gets stored stays sRGB, since that's what
    // the shader should output.

    private static Color ToLinear(Color c) =>
        new Color(ToLinear(c.r), ToLinear(c.g), ToLinear(c.b));

    private static float ToLinear(float c) =>
        c <= 0.04045f ? c / 12.92f : Mathf.Pow((c + 0.055f) / 1.055f, 2.4f);

    /// <summary>
    /// Converts linear RGB to Oklab (Björn Ottosson, 2020). Oklab is arranged so
    /// Euclidean distance approximates perceived colour difference, which means the
    /// nearest palette entry by simple distance is also the nearest by eye.
    /// </summary>
    private static Vector3 RgbToOklab(Color c)
    {
        float l = 0.4122214708f * c.r + 0.5363325363f * c.g + 0.0514459929f * c.b;
        float m = 0.2119034982f * c.r + 0.6806995451f * c.g + 0.1073969566f * c.b;
        float s = 0.0883024619f * c.r + 0.2817188376f * c.g + 0.6299787005f * c.b;

        float lc = Mathf.Pow(l, 1f / 3f);
        float mc = Mathf.Pow(m, 1f / 3f);
        float sc = Mathf.Pow(s, 1f / 3f);

        return new Vector3(
            0.2104542553f * lc + 0.7936177850f * mc - 0.0040720468f * sc,   // L, lightness
            1.9779984951f * lc - 2.4285922050f * mc + 0.4505937099f * sc,   // a, green-red
            0.0259040371f * lc + 0.7827717662f * mc - 0.8086757660f * sc);  // b, blue-yellow
    }

    // ---- Cleanup -----------------------------------------------------------------

    /// <summary>
    /// Textures hold unmanaged memory that isn't collected. With ExecuteAlways these
    /// rebuild on every assembly reload, so the old ones must go explicitly.
    /// </summary>
    private void ReleaseTextures()
    {
        Release(ref indexLut);
        Release(ref rampTexture);
    }

    private void Release<T>(ref T texture) where T : Object
    {
        if (texture == null) return;

        if (Application.isPlaying) Destroy(texture);
        else DestroyImmediate(texture);

        texture = null;
    }
}