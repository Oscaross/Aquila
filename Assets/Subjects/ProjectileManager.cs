using UnityEngine;
using System.Collections.Generic;

public class ProjectileManager : MonoBehaviour
{
    private List<Projectile> projectilesInScene = new List<Projectile>();

    public void FireProjectile(ProjectileArgs args)
    {
        Projectile p = Instantiate(args.Profile.projectilePrefab, args.Origin, Quaternion.identity, transform);
        p.Initialise(args.Profile, args);
        p.OnExpired += TryDestroyProjectile; // when the projectile "expires" it informs this manager and we try to destroy it
        
        projectilesInScene.Add(p);
    }

    // TODO: This can be conveniently refactored into an object pool later if need be
    private void TryDestroyProjectile(Projectile p)
    {
        p.OnExpired -= TryDestroyProjectile;
        Destroy(p.gameObject);
        projectilesInScene.Remove(p);
    }
}
