using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    // NOTE: Use [SerialiseField] for private fields to show up in the inspector. Public fields are in there by default.
    public Transform target; // this will just be the player's transform (position)
    [Range(0f, 0.1f), Tooltip("")]
    public float smoothTime = 0.05f; // smoothing factor, a higher smooth means the camera takes longer to follow the target as it changes position
    private Vector3 velocity; // an internal tracked property

    private void LateUpdate()
    {
        if (target == null) return;
        float x = Mathf.Round(target.position.x * 16f) / 16f;
        transform.position = new Vector3(x, transform.position.y, transform.position.z);
    }
}
