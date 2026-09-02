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
    [Tooltip("Debug mode for analysing shader lighting behaviour. ZERO for no debug. " +
             "Mode 1: ramp, every pixel that resolves to the same ramp is coloured in the same hue. " +
             "Mode 2: index, colours by position after the darkness shift regardless of ramp. Every pixel that is shaded at the same relative index in its ramp should be the same hue." +
             "Mode 3: ramp and index combined, every distinct (ramp, index) pair has its own hue, so every visually distinct entry in use is shown." +
             "Mode 4: isolate a single ramp according to debugRamp. Whichever debug ramp is selected is coloured in by index. Anything not on debugRamp is coloured in gray." +
             "Mode 100: numeric probe. Encodes each (ramp, index, rampLength) into a byte in RGB. Eyedrop the pixel to analyse and its RGB encodes three integers; R = rampIndex, G = indexInRamp, B = rampLength.")]
    [SerializeField, Range(0, 100)] private int shaderDebugMode;
    [Tooltip("The ramp index to isolate when in mode 4 of debug mode.")]
    [SerializeField, Range(0, 100)] private int debugRamp;

    [Header("Quantisation")]
    [Tooltip("Discrete alpha levels available to shaders that blend rather than clip.")]
    [SerializeField, Range(2, 32)] private int alphaSteps = 16;

    private static readonly int IndexLutID    = Shader.PropertyToID("_PaletteIndexLUT");
    private static readonly int RampTexID     = Shader.PropertyToID("_PaletteRamps");
    private static readonly int MaxRampLenID  = Shader.PropertyToID("_PaletteMaxRampLength");
    private static readonly int RampCountID   = Shader.PropertyToID("_PaletteRampCount");
    private static readonly int AlphaStepsID  = Shader.PropertyToID("_PaletteAlphaSteps");
    private static readonly int BlockCountID = Shader.PropertyToID("_PaletteBlockCount");

    private static readonly int DebugModeID = Shader.PropertyToID("_DebugMode");
    private static readonly int DebugRampID = Shader.PropertyToID("_DebugRamp");

    private Texture3D indexLut;
    private Texture2D rampTexture;

    public static float QuantiseAlpha(float value) =>
        Quantise(value, Instance != null ? Instance.alphaSteps : 16);
    
    private static float Quantise(float value, int steps) =>
        steps <= 1 ? value : Mathf.Round(value * steps) / steps;
    

    private void OnEnable()
    {
        Instance = this;
        RebuildPalette();
        PublishSettings();
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
        ReleaseTextures();
    }

    private void OnValidate() => PublishSettings();

    private void PublishSettings()
    {
        Shader.SetGlobalFloat(AlphaStepsID, alphaSteps);
        Shader.SetGlobalFloat(DebugRampID, debugRamp);
        Shader.SetGlobalFloat(DebugModeID, shaderDebugMode);
    }
    
    [ContextMenu("Rebuild Palette")]
    public void RebuildPalette()
    {
        if (palette == null) return;
        if (palette.BlockCount == 0)
        {
            Debug.LogWarning($"{name}: palette has no blocks.", this);
            return;
        }

        // Gather every block. Index in this list is the block index the shader uses.
        var blocks = new List<List<Color[]>>();
        for (int b = 0; b < palette.BlockCount; b++)
        {
            List<Color[]> ramps = palette.GetRamps(b);
            if (ramps == null || ramps.Count == 0)
            {
                Debug.LogWarning($"{name}: block '{palette.BlockName(b)}' produced no ramps.", this);
                return;
            }
            blocks.Add(ramps);
        }

        // Structure must be identical across blocks or the row arithmetic is meaningless:
        // the shader reads (block * rampCount + ramp), and an index valid in one block
        // has to be valid in every other.
        List<Color[]> reference = blocks[palette.ReferenceBlock];
        for (int b = 0; b < blocks.Count; b++)
        {
            if (blocks[b].Count != reference.Count)
            {
                Debug.LogError($"{name}: block '{palette.BlockName(b)}' has {blocks[b].Count} ramps, " +
                               $"reference has {reference.Count}. Aborting bake.", this);
                return;
            }
            for (int y = 0; y < reference.Count; y++)
                if (blocks[b][y].Length != reference[y].Length)
                {
                    Debug.LogError($"{name}: block '{palette.BlockName(b)}' ramp {y} has " +
                                   $"{blocks[b][y].Length} entries, reference has {reference[y].Length}. " +
                                   $"Aborting bake.", this);
                    return;
                }
        }

        int maxRampLength = 0;
        foreach (Color[] ramp in reference) maxRampLength = Mathf.Max(maxRampLength, ramp.Length);

        ReleaseTextures();

        rampTexture = BuildRampTexture(blocks, maxRampLength);   // now takes every block
        indexLut    = BuildIndexLut(reference);                  // reference only

        Shader.SetGlobalTexture(RampTexID, rampTexture);
        Shader.SetGlobalTexture(IndexLutID, indexLut);
        Shader.SetGlobalFloat(MaxRampLenID, maxRampLength);
        Shader.SetGlobalFloat(RampCountID, reference.Count);
        Shader.SetGlobalFloat(BlockCountID, blocks.Count);

        Debug.Log($"{name}: baked {blocks.Count} block(s) × {reference.Count} ramps, " +
                  $"longest {maxRampLength}.", this);
    }
    
    // ---- Ramp texture ------------------------------------------------------------

    /// <summary>
    /// One row per ramp, one texel per entry, left (darkest) to right (lightest).
    /// Short ramps repeat their last entry to fill the row — the shader clamps to the
    /// real length anyway, so the padding is never read, but it keeps the texture
    /// rectangular and avoids sampling undefined texels.
    /// </summary>
    private Texture2D BuildRampTexture(List<List<Color[]>> blocks, int maxRampLength)
    {
        int rampCount = blocks[0].Count;
        int height = rampCount * blocks.Count;

        var tex = new Texture2D(maxRampLength, height, TextureFormat.RGBA32, false, false)
        {
            filterMode = FilterMode.Point,
            wrapMode   = TextureWrapMode.Clamp
        };

        for (int b = 0; b < blocks.Count; b++)
        for (int y = 0; y < rampCount; y++)
        {
            Color[] ramp = blocks[b][y];
            int row = b * rampCount + y;

            for (int x = 0; x < maxRampLength; x++)
                // Pad short ramps with their last entry. Nothing should read past
                // rampLength, but a clamped read then lands on a legal colour
                // rather than whatever was left in the buffer.
                tex.SetPixel(x, row, ramp[Mathf.Min(x, ramp.Length - 1)]);
        }

        tex.Apply(false, false);
        return tex;
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
    /// Dumps every block's ramps and the hex values the bake actually produced, then verifies
    /// each authored colour in the reference block resolves back to its own ramp and index
    /// through the LUT. A colour that doesn't round-trip shares a LUT cell with an entry from
    /// another ramp — it can never be lit correctly, and raising LutSize is the fix.
    /// Non-reference blocks are checked for structural agreement instead: same ramp count,
    /// same length per ramp. A mismatch there diverges silently as the scene darkens.
    /// </summary>
    [ContextMenu("Log Palette")]
    public void LogPalette()
    {
        if (palette == null) { Debug.LogWarning($"{name}: no palette assigned.", this); return; }
        if (palette.BlockCount == 0) { Debug.LogWarning($"{name}: no blocks.", this); return; }

        int reference = palette.ReferenceBlock;
        List<Color[]> referenceRamps = null;

        for (int b = 0; b < palette.BlockCount; b++)
        {
            List<Color[]> ramps = palette.GetRamps(b);
            if (ramps == null || ramps.Count == 0)
            {
                Debug.LogWarning($"Block {b} '{palette.BlockName(b)}': no ramps.", this);
                continue;
            }

            string tag = b == reference ? " [LUT reference]" : "";
            Debug.Log($"===== Block {b}: {palette.BlockName(b)}{tag} =====");

            int total = 0;
            for (int y = 0; y < ramps.Count; y++)
            {
                var hex = new List<string>();
                foreach (Color c in ramps[y]) hex.Add(ColorUtility.ToHtmlStringRGB(c));
                total += ramps[y].Length;

                Debug.Log($"  Ramp {y} ({ramps[y].Length}): {string.Join(" ", hex)}");
            }

            Debug.Log($"  {ramps.Count} ramps, {total} colours total.");

            if (b == reference) { referenceRamps = ramps; continue; }

            // ---- Structural check against the reference ----
            if (ramps.Count != referenceRamps.Count)
            {
                Debug.LogError($"  Block '{palette.BlockName(b)}' has {ramps.Count} ramps, " +
                               $"reference has {referenceRamps.Count}.", this);
                continue;
            }

            for (int y = 0; y < ramps.Count; y++)
                if (ramps[y].Length != referenceRamps[y].Length)
                    Debug.LogError($"  Ramp {y} has {ramps[y].Length} entries, " +
                                   $"reference has {referenceRamps[y].Length}.", this);
        }

        // ---- Round-trip check, reference block only ----
        if (indexLut == null) { Debug.LogWarning("No LUT baked — run Rebuild Palette first."); return; }
        if (referenceRamps == null) { Debug.LogWarning("Reference block produced no ramps."); return; }

        Color32[] cells = indexLut.GetPixels32();
        int collisions = 0;

        for (int y = 0; y < referenceRamps.Count; y++)
        for (int x = 0; x < referenceRamps[y].Length; x++)
        {
            Color c = referenceRamps[y][x];
            int cr = Mathf.Clamp(Mathf.RoundToInt(c.r * (LutSize - 1)), 0, LutSize - 1);
            int cg = Mathf.Clamp(Mathf.RoundToInt(c.g * (LutSize - 1)), 0, LutSize - 1);
            int cb = Mathf.Clamp(Mathf.RoundToInt(c.b * (LutSize - 1)), 0, LutSize - 1);

            Color32 stored = cells[cr + cg * LutSize + cb * LutSize * LutSize];

            if (stored.r != y || stored.g != x)
            {
                collisions++;
                Debug.LogWarning(
                    $"#{ColorUtility.ToHtmlStringRGB(referenceRamps[y][x])} (ramp {y}, index {x}) " +
                    $"resolves to ramp {stored.r}, index {stored.g} " +
                    $"— cell [{cr},{cg},{cb}] is shared.");
            }
        }

        Debug.Log(collisions == 0
            ? "All reference colours round-trip correctly."
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