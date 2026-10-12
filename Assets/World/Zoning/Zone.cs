using System;
using Newtonsoft.Json;
using UnityEngine;

/// <summary>
/// The parent class for a zone, allowing for non-duplicated persistence logic and zoning logic. Child zones derive from this class, and implement their own specific logic such as
/// forest zones spawning and containing trees or legion zones containing legion-specific buildings or defences.
/// </summary>

public abstract class Zone : MonoBehaviour, IFindable
{
    /// Untyped view for code that doesn't care which kind of zone this is (ZoneManager, saving, queries).
    public abstract ZoneModel BaseModel { get; }

    public int Id => BaseModel.Id;
    public RectInt Bounds => BaseModel.Bounds;
    public abstract ZoneType Type { get; }

    public abstract void Bind(ZoneModel model, WorldContext context);
    public abstract ZoneModel CreateModel(RectInt bounds);

    /// <summary>
    /// To be called once when a new zone is first instantiated. NOT to be called when reconstructing existing save data.
    /// </summary>
    public virtual void OnZoneCreated() {}
    /// <summary>
    /// To be called once when a zone is finally destroyed and removed from records (i.e. re-allocating dependent members/reorganising the world zoning system).
    /// </summary>
    public virtual void OnZoneDestroyed() {}
}


/// <summary>
/// A wrapper for non-generic zones which allows us to specify data and the data model that a zone is bound to, for example, a ForestZoneModel that derives from ZoneModel.
/// </summary>
/// <typeparam name="TModel">The (derivative of ZoneModel) that wraps the data this specific zone object must track between play sessions.</typeparam>
public abstract class Zone<TModel> : Zone where TModel : ZoneModel, new()
{
    protected TModel Model { get; private set; }
    public override ZoneModel BaseModel => Model;

    public sealed override void Bind(ZoneModel model, WorldContext context)
    {
        if (model is not TModel typed)
            throw new ArgumentException($"{GetType().Name} expects {typeof(TModel).Name}, got {model?.GetType().Name ?? "null"}");

        Model = typed;
        OnBind(context);
    }

    public override ZoneModel CreateModel(RectInt b)
    {
        var model = new TModel();
        model.Init(Type, b);
        return model;
    }

    protected virtual void OnBind(WorldContext context) {}
}

public class ZoneModel : EntityData
{
    public ZoneType Type;
    public int Width, Height, XMin, YMin;

    [JsonIgnore] public RectInt Bounds => new RectInt(XMin, YMin, Width, Height);

    public void Init(ZoneType type, RectInt b)
    {
        Type = type;
        Width = b.width; Height = b.height;
        XMin = b.xMin;   YMin = b.yMin;
    }
}
