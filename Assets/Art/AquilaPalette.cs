using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Scriptable Objects/Palette")]
public class AquilaPalette : ScriptableObject
{
    public TextAsset paletteFile;

    [System.Serializable]
    public class Ramp
    {
        public string name;
        public int startIndex;
        public int count;
    }

    public Ramp[] ramps;
    [Range(0, 8)] public int subdivisions = 2;

    private Color[] ParseGpl()
    {
        var colours = new List<Color>();
        foreach (string line in paletteFile.text.Split('\n'))
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
    /// Expands the authored ramps into a larger set by interpolating WITHIN each ramp.
    /// Never across ramps — blending a dirt brown with a foliage green gives muddy
    /// colours that aren't in the art's language.
    /// </summary>
    public Color[] Expand()
    {
        Color[] source = ParseGpl();
        var result = new List<Color>();

        foreach (Ramp ramp in ramps)
        {
            if (ramp.count < 1) continue;

            int end = Mathf.Min(ramp.startIndex + ramp.count, source.Length);

            for (int i = ramp.startIndex; i < end - 1; i++)
            {
                result.Add(source[i]);
                for (int s = 1; s <= subdivisions; s++)
                    result.Add(Color.Lerp(source[i], source[i + 1],
                        s / (float)(subdivisions + 1)));
            }

            if (end - 1 >= ramp.startIndex) result.Add(source[end - 1]);
        }

        return result.ToArray();
    }
}