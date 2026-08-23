using UnityEngine;

/**
 * 
*/

[ExecuteAlways]
public class SkyController : MonoBehaviour
{
    public static SkyController Instance { get; private set; }

    [SerializeField] private SkyPreset preset;
    [SerializeField] private float horizonY = 0f;

    /// <summary>
    /// Current colour of the zenith (non-horizon sky).
    /// </summary>
    public Color Zenith { get; private set; }
    /// <summary>
    /// Current colour of the horizon.
    /// </summary>
    public Color Horizon { get; private set; }
    /// <summary>
    /// Current light tint in the sky.
    /// </summary>
    public Color Light { get; private set; }
    /// <summary>
    /// Current haze for haze shader to tint backgrounds with.
    /// </summary>
    public Color Haze { get; private set; }

    static readonly int HazeColorID = Shader.PropertyToID("_GlobalHazeColor");
    static readonly int HorizonYID = Shader.PropertyToID("_GlobalHorizonY");

    private void OnEnable() => Instance = this;

    private void LateUpdate()
    {
        float t = GameTime.Now;

        Zenith = preset.zenithColour.Evaluate(t);
        Horizon = preset.horizonColour.Evaluate(t);
        Light = preset.lightColour.Evaluate(t);
        Haze = preset.hazeColour.Evaluate(t);

        Shader.SetGlobalColor(HazeColorID, Haze);
        Shader.SetGlobalFloat(HorizonYID, horizonY);
    }
}