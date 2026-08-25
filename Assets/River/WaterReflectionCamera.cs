using UnityEngine;

/// <summary>
/// Mirrors the main camera about a water surface and renders into a RenderTexture
/// that the water shader samples in screen space.
/// </summary>
[ExecuteAlways]
[RequireComponent(typeof(Camera))]
[DefaultExecutionOrder(1000)] // after Pixel Perfect Camera has snapped the main camera
public class WaterReflectionCamera : MonoBehaviour
{
    [SerializeField] private Camera mainCamera;
    [SerializeField] private WaterBody water;
    [SerializeField] private RenderTexture target;

    private static readonly int ReflectionTex = Shader.PropertyToID("_WaterReflectionTex");
    private static readonly int ReflectionTexel = Shader.PropertyToID("_WaterReflectionTexelSize");
    private static readonly int WaterlineScreenY = Shader.PropertyToID("_WaterlineScreenY");

    private Camera cam;

    private void OnEnable()
    {
        cam = GetComponent<Camera>();
        cam.orthographic = true;
        cam.targetTexture = target;
    }

    private void LateUpdate()
    {
        if (mainCamera == null || water == null || target == null) return;

        // Mirror the camera position about the water surface.
        Vector3 p = mainCamera.transform.position;
        transform.position = new Vector3(p.x, 2f * water.SurfaceY - p.y, p.z);

        // Match zoom AFTER Pixel Perfect Camera may have adjusted it.
        cam.orthographicSize = mainCamera.orthographicSize;

        // Flip vertically in clip space so the RT can be sampled at the same screen UV.
        mainCamera.ResetProjectionMatrix();
        Matrix4x4 proj = mainCamera.projectionMatrix;
        proj.m11 = -proj.m11;
        cam.projectionMatrix = proj;

        Shader.SetGlobalTexture(ReflectionTex, target);
        Shader.SetGlobalVector(ReflectionTexel,
            new Vector4(1f / target.width, 1f / target.height, target.width, target.height));

        // NEW: where the waterline currently appears on screen, 0-1 from viewport bottom.
        float waterScreenY = mainCamera.WorldToViewportPoint(
            new Vector3(0f, water.SurfaceY, 0f)).y;
        Shader.SetGlobalFloat(WaterlineScreenY, waterScreenY);
    }
}