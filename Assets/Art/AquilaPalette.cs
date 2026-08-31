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
        public int count;
    }

    public Ramp[] ramps;

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
    
    public List<Color[]> GetRamps()
    {
        Color[] source = ParseGpl();
        Debug.Log($"GPL parsed: {source.Length}. Ramps array: {ramps.Length}.");

        int declared = 0;
        foreach (Ramp r in ramps) declared += r.count;
        Debug.Log($"Ramp counts sum to {declared}.");

        if (declared != source.Length)
            Debug.LogError($"Ramps cover {declared} of {source.Length} — " +
                           $"{source.Length - declared} colours unreachable.");
        
        var result = new List<Color[]>();
        
        Color[] converted;
        int idxInSource = 0;
        
        foreach (Ramp ramp in ramps)
        {
            converted = new Color[ramp.count];

            for (int i = 0; i < ramp.count; i++)
            {
                converted[i] = source[idxInSource];
                idxInSource++;
            }
            
            result.Add(converted);
        }

        return result;
    }
}