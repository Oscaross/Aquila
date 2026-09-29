using Newtonsoft.Json;

public class IdAllocator : ISaveRecord
{
    [JsonProperty] public int NextID { get; private set; }

    /// <summary>
    /// Allocate a new, unique ID to this object that the save system can use to reference it in the future.
    /// </summary>
    /// <returns></returns>
    public int AllocateNewID() => ++NextID;
}
