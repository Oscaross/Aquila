using System;
using System.Collections.Generic;
using UnityEngine;

public class ZoneManager : MonoBehaviour
{
    public List<Zone> zones = new();

    private void Awake()
    {
        CreateNewZone(new RectInt(new Vector2Int(-40, 0), new Vector2Int(16, 10)), ZoneType.Arable);
        CreateNewZone(new RectInt(new Vector2Int(-20, 0), new Vector2Int(40, 10)), ZoneType.Legion);
        CreateNewZone(new RectInt(new Vector2Int(30, 0), new Vector2Int(25, 10)), ZoneType.Forest);
    }

    private void CreateNewZone(RectInt bounds, ZoneType type)
    {
        var newZone = new Zone(bounds, type);
        zones.Add(newZone);
    }
    
    private void OnDrawGizmos()
    {
        foreach (Zone zone in zones)
        {
            RectInt r = zone.bounds;
            var centre = new Vector3(r.xMin + r.width * 0.5f, r.yMin + r.height * 0.5f, 0f);
            var size   = new Vector3(r.width, r.height, 0f);

            Color c = ColourFor(zone.type);

            Gizmos.color = new Color(c.r, c.g, c.b, 0.15f);
            Gizmos.DrawCube(centre, size);

            Gizmos.color = c;
            Gizmos.DrawWireCube(centre, size);

            #if UNITY_EDITOR
                        UnityEditor.Handles.color = c;
                        UnityEditor.Handles.Label(new Vector3(r.xMin, r.yMax + 0.5f, 0f),
                            $"{zone.type}  {r.width}×{r.height}");
            #endif
        }
    }

    private static Color ColourFor(ZoneType type) => type switch
    {
        ZoneType.Arable => Color.yellow,
        ZoneType.Forest => Color.green,
        ZoneType.Legion => Color.red,
        ZoneType.Wilderness => Color.cyan,
        _ => Color.magenta
    };
}
