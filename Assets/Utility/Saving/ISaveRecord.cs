/// <summary>
/// Any data model "...Model" class that needs to be serialised MUST implement ISaveRecord otherwise it will not be found by the binder and serialised correctly.
/// </summary>

public interface ISaveRecord
{
    // blank right now because this just exists as a flag to show that classes are saveable from one point, no functionality other than pointing to the classes that need to be serialised.
}
