using UnityEngine;
using UnityEngine.Rendering.Universal;

public class CameraFollow : MonoBehaviour
{
    public Transform target; // this will just be the player's transform (position)
    [Range(0f, 0.1f), Tooltip("")]
    public float smoothTime = 0.05f; // smoothing factor, a higher smooth means the camera takes longer to follow the target as it changes position
    private Vector3 velocity; // an internal tracked property

    private static readonly int PixelScaleID = Shader.PropertyToID("_PixelScale");
    [SerializeField] PixelPerfectCamera ppc;
    
    private void LateUpdate()
    {
        if (target == null) return;
        float x = Mathf.Round(target.position.x * 16f) / 16f;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);

        Shader.SetGlobalFloat(PixelScaleID, ppc.pixelRatio); // our sky shader needs to know what the camera's pixel upscale ratio is to correctly author the dithered sprite
    }
}
