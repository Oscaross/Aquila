using UnityEngine;

public class Legion : MonoBehaviour
{
    [SerializeField] private LegionResources resources;
    [SerializeField] private ProjectileManager projectileManager;
    [SerializeField] private LegionSubjectManager subjectManager;
    [SerializeField] private Bounds legionBounds;
    [SerializeField] private ZoneManager zoneManager;

    public LegionResources Resources => resources;
    public ProjectileManager ProjectileManager => projectileManager;
    public LegionSubjectManager SubjectManager => subjectManager;
    public Bounds LegionBounds =>  legionBounds; // TODO: This will be deprecated soon because it should be done through the zone manager
    public ZoneManager ZoneManager => zoneManager;
}
