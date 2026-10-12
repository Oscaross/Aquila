using UnityEngine;

public class ArableZone : Zone<ArableZoneModel>
{
    public override ZoneType Type => ZoneType.Arable;

    public override void OnZoneCreated()
    {
        
    }
}

public class ArableZoneModel : ZoneModel
{
    
}