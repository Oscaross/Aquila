using UnityEngine;

public class Legion : MonoBehaviour
{
    [SerializeField] private LegionResources resources;
    [SerializeField] private ProjectileManager projectileManager;
    [SerializeField] private LegionSubjectManager subjectManager;
    [SerializeField] private ZoneManager zoneManager;
    [SerializeField] private Player player;

    public LegionResources Resources => resources;
    public ProjectileManager ProjectileManager => projectileManager;
    public LegionSubjectManager SubjectManager => subjectManager;
    public ZoneManager ZoneManager => zoneManager;
    public Player Player => player;
}
