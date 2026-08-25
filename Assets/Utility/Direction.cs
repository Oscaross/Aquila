public enum Direction
{
    Left = -1,
    Right = 1
}

public static class DirectionExtensions
{
    /// <summary>-1 for Left, +1 for Right.</summary>
    public static int Sign(this Direction d) => (int)d;

    public static Direction Opposite(this Direction d) =>
        d == Direction.Left ? Direction.Right : Direction.Left;

    /// <summary>Direction implied by a horizontal delta. Zero resolves to Right.</summary>
    public static Direction FromDelta(float dx) =>
        dx < 0f ? Direction.Left : Direction.Right;
}