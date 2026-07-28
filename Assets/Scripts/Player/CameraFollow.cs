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
        if (target == null) return; // can't track a null target, this helps us avoid null pointers (otherwise target.position maybe throws)

        Vector3 goal = new Vector3(target.position.x, transform.position.y, transform.position.z); // transform.position.z is our z position, we don't really care about it since this is a 2D game with only two axes
        transform.position = Vector3.SmoothDamp(transform.position, goal, ref velocity, smoothTime); // computes an acceleration that brings the camera towards its target smoothly
    }
}
