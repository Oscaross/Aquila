using UnityEngine;

/// <summary>
/// Owns the project's colour discipline. Expands the authored palette, bakes a
/// 3D lookup texture mapping every RGB cell to its nearest palette entry, and
/// publishes it globally so every Aquila shader snaps through the same table.
///
/// The LUT is rebuilt on enable and on demand, never per frame — the bake is a
/// few hundred milliseconds and the result only changes when the palette does.
/// </summary>
[ExecuteAlways]
public class ShaderController : MonoBehaviour
{
    private const int LutSize = 32;   // cells per axis; 32^3 = 32,768 entries, 128 KB

    [SerializeField] private AquilaPalette palette;
    [Tooltip("Discrete alpha levels available to shaders that blend rather than clip.")]
    [SerializeField, Range(2, 16)] private int alphaSteps = 8;

    private static readonly int PaletteLutID = Shader.PropertyToID("_PaletteLUT");
    private static readonly int AlphaStepsID = Shader.PropertyToID("_PaletteAlphaSteps");

    private Texture3D lut;

    private void OnEnable() => RebuildPalette();

    private void OnDisable() => ReleaseLut();

    private void LateUpdate()
    {
        Shader.SetGlobalFloat(AlphaStepsID, alphaSteps);
    }

    /// <summary>
    /// Rebuilds the lookup texture from the palette asset. Call after editing
    /// the palette — OnValidate here won't catch changes made to the asset itself.
    /// </summary>
    [ContextMenu("Rebuild Palette")]
    public void RebuildPalette()
    {
        if (palette == null) return;

        Color[] expanded = palette.Expand();
        if (expanded.Length == 0)
        {
            Debug.LogWarning($"{name}: palette expanded to zero colours — check the ramp definitions.", this);
            return;
        }

        ReleaseLut();
        lut = BuildLut(expanded);
        Shader.SetGlobalTexture(PaletteLutID, lut);

        Debug.Log($"{name}: palette LUT baked from {expanded.Length} colours.", this);
    }

    /// <summary>
    /// For each cell of a 32x32x32 RGB cube, stores the nearest palette colour.
    /// Doing the search once here means the shader is a single texture sample,
    /// so palette size stops affecting frame cost entirely.
    /// </summary>
    private static Texture3D BuildLut(Color[] colours)
    {
        var texture = new Texture3D(LutSize, LutSize, LutSize, TextureFormat.RGBA32, false)
        {
            filterMode = FilterMode.Point,   // bilinear would blend between cells and
            wrapMode = TextureWrapMode.Clamp, // produce colours outside the palette
            hideFlags = HideFlags.HideAndDontSave
        };

        var pixels = new Color32[LutSize * LutSize * LutSize];

        for (int b = 0; b < LutSize; b++)
        for (int g = 0; g < LutSize; g++)
        for (int r = 0; r < LutSize; r++)
        {
            Color input = new Color(r / (LutSize - 1f), g / (LutSize - 1f), b / (LutSize - 1f));

            float bestDistance = float.MaxValue;
            Color best = input;

            foreach (Color candidate in colours)
            {
                float dr = input.r - candidate.r;
                float dg = input.g - candidate.g;
                float db = input.b - candidate.b;
                float distance = dr * dr + dg * dg + db * db;

                if (distance < bestDistance)
                {
                    bestDistance = distance;
                    best = candidate;
                }
            }

            // Red varies fastest — the layout Texture3D expects.
            pixels[r + g * LutSize + b * LutSize * LutSize] = best;
        }

        texture.SetPixels32(pixels);
        texture.Apply();
        return texture;
    }

    /// <summary>
    /// Texture3D holds unmanaged memory that isn't collected. With ExecuteAlways
    /// this rebuilds on every assembly reload, so the old one must go explicitly.
    /// </summary>
    private void ReleaseLut()
    {
        if (lut == null) return;

        if (Application.isPlaying) Destroy(lut);
        else DestroyImmediate(lut);

        lut = null;
    }
}