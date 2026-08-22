using UnityEngine;

/**
   * Ensures that the haze shader is able to tint the haze effect it applies to background assets with the correct sky colour. Execute always means we can see this in the editor too
*/

[ExecuteAlways]
public class HazeController : MonoBehaviour
{
    [SerializeField] TimeOfDay timeOfDay;
    [SerializeField, Range(0f, 1f)] float skyBlend = 0.15f;
    [SerializeField] Color skyTint = new Color(0.75f, 0.83f, 0.95f);

    static readonly int HazeColorID = Shader.PropertyToID("_GlobalHazeColor");

    void LateUpdate()
    {
        Color horizon = timeOfDay.CurrentHorizonColour;
        Color haze = Color.Lerp(horizon, skyTint, skyBlend);
        Shader.SetGlobalColor(HazeColorID, haze);
    }
}