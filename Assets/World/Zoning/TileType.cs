public enum TileLayer
{
    Foreground = 0,
    Ground = 1,
    NearBackground = 2,
    MidBackground = 3
}

public static class TileLayerExtensions
{
    private static readonly TileLayerAttributes ForegroundAttributes = new TileLayerAttributes(0f, 100, 0);
    private static readonly TileLayerAttributes GroundAttributes = new TileLayerAttributes(0f, 0, 0f);
    private static readonly TileLayerAttributes NearBackgroundAttributes = new TileLayerAttributes(0.2f, -50, 0.1f);
    private static readonly TileLayerAttributes MidBackgroundAttributes = new TileLayerAttributes(0.5f, -100, 0.3f);
    
    public static TileLayerAttributes GetAttributes(this TileLayer layer) => layer switch
    {
        TileLayer.Foreground => ForegroundAttributes,
        TileLayer.Ground => GroundAttributes,
        TileLayer.NearBackground => NearBackgroundAttributes,
        TileLayer.MidBackground => MidBackgroundAttributes,
        _ => throw new System.ArgumentOutOfRangeException(nameof(layer), layer, null)
    };
}

public struct TileLayerAttributes
{
    public readonly float Haze;
    public readonly int SortingLayer;
    public readonly float ParallaxFactor;

    public TileLayerAttributes(float haze, int sortingLayer, float parallaxFactor)
    {
        Haze = haze;
        SortingLayer = sortingLayer;
        ParallaxFactor = parallaxFactor;
    }
}