/**
 * Global source of truth for game constants.
*/

public static class GlobalConstants
{
    public static int PixelsPerUnit = 16;
    public static int GroundY = 0;
    /// <summary>
    /// Each "decorative tile", which is a section of world the player walks on or a background must be the same width as zones. All zones must be a multiple of this, or else we'd be unable to generate the world.
    /// </summary>
    public static int ZoneWidthMultipleWorldUnits = 16;
    /// <summary>
    /// An arbitrarily large vertical size to prevent errors when we go over a zone. If they have a vertical component in the future then I'll change this.
    /// </summary>
    public static int ZoneHeightWorldUnits = 1000;
}
