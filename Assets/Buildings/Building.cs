using Newtonsoft.Json;
using UnityEngine;

public class Building : MonoBehaviour, IEntity<BuildingModel>, IFindable
{
    public int Id => data.Id;
    
    private BuildingType constraints;
    public BuildingType Constraints => constraints;

    private BuildingModel data;

    public void Bind(BuildingModel model)
    {
        data = model;
        constraints = model.Constraints;
    }

    public RectInt FootprintInWorldSpace => new(data.XMin, data.YMin, constraints.width, constraints.height);
}

public class BuildingModel : EntityData
{
    public BuildingModel(string typeKey, int xMin, int yMin)
    {
        TypeKey = typeKey;
        XMin = xMin;
        YMin = yMin;
    }

    public readonly string TypeKey;
    public int XMin;
    public int YMin;

    [JsonIgnore] public BuildingType Constraints => ContentRegistry.Get<BuildingType>(TypeKey);
}
