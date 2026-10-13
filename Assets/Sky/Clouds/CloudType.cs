/// <summary>
/// Defines how clouds should behave when they are rendered in the sky. Far clouds are smaller sprites and drift slower + move slower with parallax, and the contrary for near clouds.
/// </summary>

public enum CloudType
{
    Near = 0,
    Mid = 1,
    Far = 2
}