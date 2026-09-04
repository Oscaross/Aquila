using UnityEngine;

public static class BallisticSolver
{
    private const int SamplesPerWorldUnit = 4; // how many unique points we linecast between per world unit of a projectile's arc
    private const int MaxSamples = 30; // the maximum number of points we can linecast between for any projection
    private const int MinSamples = 8;
        
    /// <summary>
    /// Given some origin, target and angle for projection computes the required velocity to hit the target from origin at angle.
    /// </summary>
    /// <param name="origin">Where the shot originates from.</param>
    /// <param name="target">Where the target is.</param>
    /// <param name="thetaRad">The angle of projection, in radians.</param>
    /// <param name="gravity">The gravity in world units (m/s^-2).</param>
    /// <param name="maxSpeed">The maximum value that the velocity can take.</param>
    /// <param name="initialVelocity">The Vector2 to assign the resolved velocity vector to.</param>
    /// <returns>True if the velocity could be resolved, false if no such velocity exists for the given parameters.</returns>
    public static bool TrySolveVelocity(Vector2 origin, Vector2 target, float thetaRad, float gravity, float maxSpeed, out Vector2 initialVelocity)
    {
        initialVelocity = default;

        float dx = target.x - origin.x;
        float dy = target.y - origin.y;

        float absDx = Mathf.Abs(dx);

        if (absDx < 0.001f) return false; // can't hit a target that's minutely far away

        float tan = Mathf.Tan(thetaRad);
        float sin = Mathf.Sin(thetaRad);
        float cos = Mathf.Cos(thetaRad);

        // Find the required magnitude of the velocity, which is the solution to the equation:
        // U² = gx² / (2cos²θ · (x·tanθ − y))
        float denom = 2 * cos * cos * (absDx * tan - dy);
        float u = Mathf.Sqrt(-gravity * dx * dx / denom);
        Debug.Assert(!float.IsNaN(u));

        if (u > maxSpeed || denom <= 0f) return false; // we can't reach it because it'd exceed our maximum speed OR the target sits above the line the launch angle points along

        // Now we have the required magnitude of velocity, decompose into its horizontal (u) and vertical (v) components using theta to compose the final vector.

        float hor = Mathf.Sign(dx) * u * cos;
        float vert = u * sin;

        initialVelocity = new Vector2(hor, vert);
        return true;
    }

    public static bool IsPathClear(Vector2 origin, Vector2 u, Vector2 target, float gravity, LayerMask obstacleMask)
    {
        float dx = target.x - origin.x;
        
        float absDx = Mathf.Abs(dx);
        
        int samples = Mathf.Clamp(Mathf.CeilToInt(absDx * SamplesPerWorldUnit), MinSamples, MaxSamples);
        float stepDx = dx / samples;

        Vector2 prev = origin; // where we projected last

        for (int i = 1; i <= samples; i++)
        {
            float x = stepDx * i; // the horizontal distance into the arc this iteration
            var curr = new Vector2(origin.x + x, origin.y + GetYDisplacement(x, gravity, u));

            if (Physics2D.Linecast(prev, curr, obstacleMask)) return false; // we hit something in the obstacle mask so this is not clear
            prev = curr;
        }

        return true;
    }

    /// <summary>
    /// Computes the vertical displacement (y coordinate) of a particle under projectile motion, relative to the origin it was projected from. 
    /// </summary>
    /// <param name="dx">The relative horizontal displacement, i.e. dx = 5 means 5 world units from where the particle was first projected.</param>
    /// <param name="gravity">The gravity in metres/sec squared.</param>
    /// <param name="initialVelocity">The initial velocity vector that the particle was projected with.</param>
    /// <returns>The y displacement in world units.</returns>
    public static float GetYDisplacement(float dx, float gravity, Vector2 initialVelocity)
    {
        float ux = initialVelocity.x;
        return (initialVelocity.y / ux) * dx + (0.5f * gravity / (ux * ux)) * dx * dx;
    }

    /// <summary>
    /// Computes the horizontal displacement (x coordinate) of a particle under projectile motion, relative to the origin it was projected from.
    /// </summary>
    /// <param name="t">The time in seconds elapsed since projection.</param>
    /// <param name="initialVelocity">The initial velocity vector that the particle was projected with.</param>
    /// <returns>The x displacement in world units.</returns>
    public static float GetXDisplacement(float t, Vector2 initialVelocity)
    {
        return t * initialVelocity.x;
    }

    public static Vector2 GetWorldPositionAtTime(Vector2 origin, float t, Vector2 initialVelocity, float gravity)
    {
        return origin + new Vector2(
            initialVelocity.x * t,
            initialVelocity.y * t + 0.5f * gravity * t * t);
    }

    // TODO: Make scatter work properly rather than just returning the target
    public static Vector2 ApplyScatter(Vector2 from, Vector2 to, float accuracy, ProjectileProfile profile)
    {
        return to;
    }
}
