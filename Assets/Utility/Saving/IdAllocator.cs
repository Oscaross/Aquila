using Newtonsoft.Json;

public class IdAllocator : ISaveRecord
{
    [JsonProperty] public int PreviousID { get; private set; }

    /// <summary>
    /// Allocate a new, unique ID to this object that the save system can use to reference it in the future.
    /// </summary>
    /// <returns>The new ID.</returns>
    public int AllocateNewID() => ++PreviousID;
}
