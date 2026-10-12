using Newtonsoft.Json;

public class ZoneState
{
    public EntityStore<ZoneModel> Zones = new();
    public EntityStore<FellableTreeData> FellableTrees = new();
}