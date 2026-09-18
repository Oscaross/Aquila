using UnityEngine;

public class Legion : MonoBehaviour
{
    [SerializeField] private LegionResources resources;
    [SerializeField] private ProjectileManager projectileManager;
    [SerializeField] private LegionSubjectManager subjectManager;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private BuildingManager buildingManager;
    [SerializeField] private Player player;

    public LegionResources Resources => resources;
    public ProjectileManager ProjectileManager => projectileManager;
    public LegionSubjectManager SubjectManager => subjectManager;
    public ZoneManager ZoneManager => zoneManager;
    public BuildingManager BuildingManager => buildingManager;
    public Player Player => player;
}
