using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Experimental.Rendering;

[ExecuteAlways]
public class ShaderController : MonoBehaviour
{
    private const int LutSize = 64; // cells per axis; n^3 entries
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
    
    #if UNITY_EDITOR
    private const string LutPath  = "Assets/Shaders/Generated/PaletteIndexLUT.asset";
    private const string RampPath = "Assets/Shaders/Generated/PaletteRamps.asset";
    #endif

    [SerializeField] private Texture3D indexLut;
    [SerializeField] private Texture2D rampTexture;
    
    [SerializeField, HideInInspector] private int maxRampLength;
    [SerializeField, HideInInspector] private int rampCount;
    [SerializeField, HideInInspector] private int blockCount;

    public static float QuantiseAlpha(float value) =>
        Quantise(value, Instance != null ? Instance.alphaSteps : 16);
    
    private static float Quantise(float value, int steps) =>
        steps <= 1 ? value : Mathf.Round(value * steps) / steps;
    

    private void OnEnable()
    {
        Instance = this;
        
        if (indexLut == null || rampTexture == null)
        {
            Debug.LogWarning($"{name}: palette not baked — run Rebuild Palette.", this);
            return;
        }

        PublishTextures();
        PublishSettings();
    }
    
    private void PublishTextures()
    {
        Shader.SetGlobalTexture(RampTexID, rampTexture);
        Shader.SetGlobalTexture(IndexLutID, indexLut);
        Shader.SetGlobalFloat(MaxRampLenID, maxRampLength);
        Shader.SetGlobalFloat(RampCountID, rampCount);
        Shader.SetGlobalFloat(BlockCountID, blockCount);
        
        Debug.Assert(Shader.GetGlobalFloat(RampCountID) > 0, "_PaletteRampCount published as 0");
        Debug.Assert(Shader.GetGlobalFloat(BlockCountID) > 0, "_PaletteBlockCount published as 0");
    }

    private void OnDisable()
    {
        if (Instance == this) Instance = null;
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
        if (palette == null) { Debug.LogWarning($"{name}: no palette assigned.", this); return; }

        var temperatures = (LightTemperature[])Enum.GetValues(typeof(LightTemperature));

        // Gather every block. Position in this list is the block index the shader uses, and it
        // matches the LightTemperature value because we walk the enum in declaration order.
        var blocks = new List<List<Color[]>>(temperatures.Length);
        foreach (LightTemperature temp in temperatures)
        {
            List<Color[]> ramps = palette.GetRamps(temp);

            if (ramps == null || ramps.Count == 0)
            {
                Debug.LogError($"{name}: light temperature '{temp}' produced no ramps. " +
                               $"Every temperature needs a GPL assigned on the palette, or block " +
                               $"indices will not line up with LightTemperature values. Aborting bake.", this);
                return;
            }

            blocks.Add(ramps);
        }

        // Structure must be identical across blocks or the row arithmetic is meaningless:
        // the shader reads (block * rampCount + ramp), and an index valid in one block
        // has to be valid in every other.
        LightTemperature referenceTemp = palette.ReferenceBlock;
        List<Color[]> reference = blocks[(int)referenceTemp];

        for (int b = 0; b < blocks.Count; b++)
        {
            LightTemperature temp = temperatures[b];

            if (blocks[b].Count != reference.Count)
            {
                Debug.LogError($"{name}: block '{temp}' has {blocks[b].Count} ramps, " +
                               $"reference '{referenceTemp}' has {reference.Count}. " +
                               $"Aborting bake.", this);
                return;
            }

            for (int y = 0; y < reference.Count; y++)
                if (blocks[b][y].Length != reference[y].Length)
                {
                    Debug.LogError($"{name}: block '{temp}' ramp {y} has " +
                                   $"{blocks[b][y].Length} entries, reference " +
                                   $"'{referenceTemp}' has {reference[y].Length}. " +
                                   $"Aborting bake.", this);
                    return;
                }
        }

        maxRampLength = 0;
        foreach (Color[] ramp in reference) maxRampLength = Mathf.Max(maxRampLength, ramp.Length);

        rampCount  = reference.Count;
        blockCount = blocks.Count;

        rampTexture = BuildRampTexture(blocks, maxRampLength);   // every block
        indexLut    = BuildIndexLut(reference);                  // reference only

        PublishTextures();
        Debug.Log($"{name}: baked {blocks.Count} block(s) × {reference.Count} ramps, " +
                  $"longest {maxRampLength}. Reference: {referenceTemp}.", this);

    #if UNITY_EDITOR
        System.IO.Directory.CreateDirectory("Assets/Shaders/Generated");
        UnityEditor.AssetDatabase.CreateAsset(indexLut, LutPath);
        UnityEditor.AssetDatabase.CreateAsset(rampTexture, RampPath);
        UnityEditor.AssetDatabase.SaveAssets();
        UnityEditor.EditorUtility.SetDirty(this);
    #endif
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

        LightTemperature reference = palette.ReferenceBlock;

        // Fetch the reference up front: the enum may visit other blocks first, and every
        // structural comparison below needs it already resolved.
        List<Color[]> referenceRamps = palette.GetRamps(reference);
        if (referenceRamps == null || referenceRamps.Count == 0)
        {
            Debug.LogError($"{name}: reference block '{reference}' produced no ramps. " +
                           $"Nothing else can be verified against it.", this);
            return;
        }

        foreach (LightTemperature temp in System.Enum.GetValues(typeof(LightTemperature)))
        {
            List<Color[]> ramps = temp == reference ? referenceRamps : palette.GetRamps(temp);
            if (ramps == null || ramps.Count == 0)
            {
                Debug.LogWarning($"Block '{temp}': produced no ramps.", this);
                continue;
            }

            LogBlock(temp, ramps, temp == reference);

            if (temp != reference)
                CompareStructure(temp, ramps, referenceRamps);
        }

        VerifyRoundTrip(referenceRamps);
    }

    /// <summary>Dumps one block's ramps as hex, one line per ramp.</summary>
    private void LogBlock(LightTemperature temp, List<Color[]> ramps, bool isReference)
    {
        string tag = isReference ? " [LUT reference]" : "";
        Debug.Log($"{temp}{tag} =====", this);

        int total = 0;
        for (int y = 0; y < ramps.Count; y++)
        {
            var hex = new List<string>(ramps[y].Length);
            foreach (Color c in ramps[y]) hex.Add(ColorUtility.ToHtmlStringRGB(c));
            total += ramps[y].Length;

            Debug.Log($"  Ramp {y} ({ramps[y].Length}): {string.Join(" ", hex)}", this);
        }

        Debug.Log($"  {ramps.Count} ramps, {total} colours total.", this);
    }

    /// <summary>
    /// Every block must be the same shape, because the shader resolves one (ramp, index) pair
    /// and uses it against whichever block the light temperature selects.
    /// </summary>
    private void CompareStructure(LightTemperature temp, List<Color[]> ramps, List<Color[]> referenceRamps)
    {
        if (ramps.Count != referenceRamps.Count)
        {
            Debug.LogError($"  Block '{temp}' has {ramps.Count} ramps, " +
                           $"reference has {referenceRamps.Count}.", this);
            return;
        }

        for (int y = 0; y < ramps.Count; y++)
            if (ramps[y].Length != referenceRamps[y].Length)
                Debug.LogError($"  Block '{temp}' ramp {y} has {ramps[y].Length} " +
                               $"entries, reference has {referenceRamps[y].Length}.", this);
    }

    /// <summary>
    /// Pushes every reference-block colour through the baked LUT and checks it comes back as
    /// itself. Cross-ramp collisions are fatal to lighting; same-ramp drift is expected once
    /// stepsBetweenColours subdivides the authored entries.
    /// </summary>
    private void VerifyRoundTrip(List<Color[]> referenceRamps)
    {
        if (indexLut == null) { Debug.LogWarning("No LUT baked — run Rebuild Palette first.", this); return; }

        Color32[] cells = indexLut.GetPixels32();

        int crossRamp = 0;
        int sameRamp = 0;
        int worstDrift = 0;
        int totalEntries = 0;

        for (int y = 0; y < referenceRamps.Count; y++)
        {
            totalEntries += referenceRamps[y].Length;

            for (int x = 0; x < referenceRamps[y].Length; x++)
            {
                Color c = referenceRamps[y][x];
                int cr = Mathf.Clamp(Mathf.RoundToInt(c.r * (LutSize - 1)), 0, LutSize - 1);
                int cg = Mathf.Clamp(Mathf.RoundToInt(c.g * (LutSize - 1)), 0, LutSize - 1);
                int cb = Mathf.Clamp(Mathf.RoundToInt(c.b * (LutSize - 1)), 0, LutSize - 1);

                Color32 stored = cells[cr + cg * LutSize + cb * LutSize * LutSize];

                if (stored.r == y && stored.g == x) continue;

                if (stored.r != y)
                {
                    // Serious: this colour resolves into a different ramp, so lighting it walks
                    // the wrong value progression entirely.
                    crossRamp++;
                    string hex = ColorUtility.ToHtmlStringRGB(c);
                    Debug.LogWarning(
                        $"CROSS-RAMP  #{hex} (ramp {y}, index {x}) resolves to ramp {stored.r}, " +
                        $"index {stored.g}; cell [{cr},{cg},{cb}] is shared with another ramp.", this);
                }
                else
                {
                    // Expected with subdivisions: adjacent entries on one ramp are close enough
                    // to land in the same cell. Harmless while the drift stays small.
                    sameRamp++;
                    worstDrift = Mathf.Max(worstDrift, Mathf.Abs(stored.g - x));
                }
            }
        }

        if (crossRamp > 0)
            Debug.LogError($"{crossRamp} CROSS-RAMP collision(s) of {totalEntries} entries. " +
                           $"These cannot be lit correctly — nudge the offending colours apart " +
                           $"in Aseprite, or raise LutSize (currently {LutSize}; memory is cubic).", this);
        else
            Debug.Log($"No cross-ramp collisions across {totalEntries} entries.", this);

        if (sameRamp > 0)
            Debug.Log($"{sameRamp} same-ramp drift(s), worst {worstDrift} index/indices. " +
                      $"Expected with stepsBetweenColours > 0 — intermediates genuinely share cells.", this);
    }
        
    [ContextMenu("Trace Dirt Colours")]
    public void TraceColours()
    {
        if (indexLut == null)
        {
            Debug.Log("Can't trace colours because there is no LUT to index from!", this);
            return;
        }
        
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
}