using UnityEngine;

public class OriginMarker : MonoBehaviour
{
    private void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(new Vector3(-100, 0, 0), new Vector3(100, 0, 0));
        Gizmos.color = Color.green;
        Gizmos.DrawLine(new Vector3(0, -100, 0), new Vector3(0, 100, 0));
    }
}