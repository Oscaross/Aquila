using UnityEngine;

public readonly struct ProjectileArgs
{
    public readonly Vector2 Origin;     
    public readonly Vector2 InitialVelocity;
    public readonly ProjectileProfile Profile;
    public readonly LayerMask TargetMask;
    public readonly LayerMask ObstacleMask;
    public readonly System.Action<Collider2D, Vector2> OnHit;
    public readonly System.Action<Vector2> OnMiss;

    public ProjectileArgs(Vector2 origin, Vector2 initialVelocity, ProjectileProfile profile,
        LayerMask targetMask, LayerMask obstacleMask,
        System.Action<Collider2D, Vector2> onHit = null,
        System.Action<Vector2> onMiss = null)
    {
        Origin = origin;
        InitialVelocity = initialVelocity;
        Profile = profile;
        TargetMask = targetMask;
        ObstacleMask = obstacleMask;
        OnHit = onHit;
        OnMiss = onMiss;
    }
}