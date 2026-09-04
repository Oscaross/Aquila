using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Responsible for holding data on colour-perfect RGBA snapping for the world. All colour ramps are programmed in here, and the engine uses this object to know which colour each pixel should be
/// based on its indexed lighting value and its light colour profile. Palettes are serialised here through Aseprite, meaning the palette the engine uses is just an extension of the same palette that
/// we author all of our sprites in.
/// </summary>

[CreateAssetMenu(menuName = "Scriptable Objects/Palette")]
public class AquilaPalette : ScriptableObject
{
    [Tooltip("Within each ramp, linearly interpolates between each colour this number of additional steps. The higher this value, the less sharp the steps are between darker colours and their lighter counterparts.")]
    [SerializeField, Range(0, 8)] public int stepsBetweenColours = 2;

    [System.Serializable]
    public class Ramp
    {
        public string name;
        public int count;
    }
    [System.Serializable]
    public class PaletteBlock
    {
        public LightTemperature temperature;
        public TextAsset gpl;
    }
    
    [Tooltip("A block is a whole palette that is hue shifted. For example, the warm block is just a list of all on-palette colours but hue-shifted towards a warm orange light. [IMPORTANT] THIS LIST IS ORDER DEPENDENT. YOU MUST DECLARE BLOCKS IN THE ORDER THEY SHOULD PROGRESS, THE TOP OF THE LIST BEING THE COOLEST AND THE BOTTOM BEING THE WARMEST.")]
    [SerializeField] private List<PaletteBlock> blocks;

    [Tooltip("A ramp is a sequence of colours within a block that form a light progression, for example, a wood ramp with dark brown progressing to mid brown and lastly a lighter brown. [IMPORTANT]: THIS LIST IS ORDER DEPENDENT. YOU MUST DECLARE RAMPS IN THE ORDER THAT THEY OCCUR IN THE PALETTE GPL, DARKEST -> LIGHTEST AND ALL RAMPS IN THE ORDER THEY ARE DECLARED!")]
    public Ramp[] ramps;

    /// <summary>
    /// Helper that opens a .txt GPL file and parses it.
    /// </summary>
    /// <returns>Colour objects of the colours found in this file.</returns>
    private Color[] ParseGpl(TextAsset gpl)
    {
        var colours = new List<Color>();
        
        foreach (string line in gpl.text.Split('\n'))
        {
            string s = line.Trim();
            if (s.Length == 0 || s.StartsWith("#") || s.StartsWith("GIMP")
                || s.StartsWith("Name:") || s.StartsWith("Columns:")) continue;

            string[] parts = s.Split(new[] { ' ', '\t' }, System.StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 3) continue;

            if (int.TryParse(parts[0], out int r)
                && int.TryParse(parts[1], out int g)
                && int.TryParse(parts[2], out int b))
                colours.Add(new Color(r / 255f, g / 255f, b / 255f));
        }
        return colours.ToArray();
    }
    
    /// <summary>
    /// Opens the Palette GPL file and parses it for the colours present. Then, generates the extra colours according to the number of steps between colours.
    /// Example: the wood ramp has 6 colours on palette from a dark brown to a light brown. The system opens the wood ramp, reads it then generates evenly interpolated steps between each wood colour and its lighter neighbour,
    /// returning the whole wood ramp with all 6 colours plus their discrete steps between. It then moves onto the next ramp, e.g. the gold ramp and does this for the whole palette.
    /// </summary>
    /// <returns>A list of ramps, where a ramp is just a colour array of all on-palette colours that a pixel can occupy in this specific ramp.</returns>
    public List<Color[]> GetRamps(LightTemperature forTemp)
    {
        // Cast a LightTemperature to a block idx.
        int block = (int)forTemp;
        
        Color[] source = ParseGpl(blocks[block].gpl);
        Debug.Log($"GPL parsed: {source.Length}. Ramps array: {ramps.Length}.");

        int declared = 0;
        foreach (Ramp r in ramps) declared += r.count;
        Debug.Log($"Ramp counts sum to {declared}.");

        if (declared != source.Length)
            Debug.LogError($"Block {blocks[block].temperature} cover {declared} of {source.Length} — " +
                           $"{source.Length - declared} colours unreachable.");
        
        var result = new List<Color[]>();
        int idxInSource = 0;
        foreach (Ramp ramp in ramps)
        {
            // Read this ramp's authored colours first.
            var authored = new Color[ramp.count];
            for (int i = 0; i < ramp.count; i++)
                authored[i] = source[idxInSource++];

            // Then expand: every gap between adjacent entries gains stepsBetweenColours.
            var expanded = new List<Color>();
            for (int i = 0; i < authored.Length - 1; i++)
            {
                expanded.Add(authored[i]);
                for (int j = 1; j <= stepsBetweenColours; j++)
                    expanded.Add(Color.Lerp(authored[i], authored[i + 1],
                        j / (float)(stepsBetweenColours + 1)));
            }
            expanded.Add(authored[^1]);   // last authored colour has no gap after it

            result.Add(expanded.ToArray());
        }

        return result;
    }

    private void OnValidate()
    {
        if (blocks.Count != System.Enum.GetValues(typeof(LightTemperature)).Length)
            Debug.LogError($"Palette has {blocks.Count} blocks; every temperature must be authored " +
                           $"or block indices will not match LightTemperature values.", this);

        for (int i = 0; i < blocks.Count; i++)
            if ((int)blocks[i].temperature != i)
                Debug.LogError($"Block at index {i} is {blocks[i].temperature}, " +
                               $"expected {((LightTemperature)i)}.", this);
    }

    public int BlockCount => System.Enum.GetValues(typeof(LightTemperature)).Length;
    /// <summary>
    /// This is the block that the LUT uses to map colours from RGBA space to Indexed space. 
    /// </summary>
    public LightTemperature ReferenceBlock => LightTemperature.NEUTRAL;
}