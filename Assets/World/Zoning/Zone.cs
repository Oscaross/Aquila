using UnityEngine;

public class Zone : MonoBehaviour, IEntity<ZoneModel>, IFindable
{
    private ZoneModel data;

    public int Id { get; private set; }

    public void Bind(ZoneModel model)
    {
        throw new System.NotImplementedException();
    }

    public RectInt Bounds => new(new Vector2Int(data.XMin, data.YMin), new Vector2Int(data.Width, data.Height));
    
    public virtual void OnZoneCreated() {}
    public virtual void OnZoneDestroyed() {}

    /// <summary>
    /// The type of zone this is, wilderness if none is specified.
    /// </summary>
    public virtual ZoneType Type => ZoneType.Wilderness;
}

public class ZoneModel : ISaveRecord
{
    public ZoneType Type;
    public int Width;
    public int Height;
    public int XMin;
    public int YMin;
}
