using UnityEngine;

public class Legion : MonoBehaviour
{
    [SerializeField] private LegionResources resources;
    [SerializeField] private ProjectileManager projectileManager;

    public LegionResources Resources => resources;
    public ProjectileManager ProjectileManager => projectileManager;
}
