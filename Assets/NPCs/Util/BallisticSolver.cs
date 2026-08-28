using UnityEngine;

public static class BallisticSolver
{
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

        if (u > maxSpeed || denom <= 0f) return false; // we can't reach it because it'd exceed our maximum speed OR the target sits above the line the launch angle points along

        // Now we have the required magnitude of velocity, decompose into its horizontal (u) and vertical (v) components using theta to compose the final vector.

        float hor = Mathf.Sign(dx) * u * cos;
        float vert = dy * u * sin;

        initialVelocity = new Vector2(hor, vert);
        return true;
    }

    public static bool IsPathClear(Vector2 origin, Vector2 u, Vector2 target, float gravity, int samples, LayerMask mask)
    {

    }
}
