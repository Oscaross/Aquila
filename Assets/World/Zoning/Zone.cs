using Newtonsoft.Json;
using UnityEngine;

public class Zone : MonoBehaviour, IEntity<ZoneModel>, IFindable
{
    private ZoneModel data;

    public int Id => data.Id;

    public virtual void Bind(ZoneModel model)
    {
        data = model;
    }

    public RectInt Bounds => data.Bounds;
    
    public virtual void OnZoneDestroyed() {}

    /// <summary>
    /// The type of zone this is, wilderness if none is specified.
    /// </summary>
    public virtual ZoneType Type => ZoneType.Wilderness;
}

public class ZoneModel : EntityData
{
    public ZoneModel(ZoneType type, int width, int height, int xMin, int yMin)
    {
        Type = type;
        Width = width;
        Height = height;
        XMin = xMin;
        YMin = yMin;
    }
    
    public ZoneType Type;
    public int Width;
    public int Height;
    public int XMin;
    public int YMin;
    
    [JsonIgnore] public RectInt Bounds => new RectInt(XMin, YMin, Width, Height);
}
