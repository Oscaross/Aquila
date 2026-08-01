using UnityEngine;

[RequireComponent(typeof(Camera))]
public class IntegerZoomCamera : MonoBehaviour
{
    [SerializeField] int referenceHeight = 360;
    [SerializeField] int ppu = 16;             
    Camera cam;

    void Awake() => cam = GetComponent<Camera>();

    void LateUpdate()
    {
        int zoom = Mathf.Max(1, Screen.height / referenceHeight);
        cam.orthographicSize = Screen.height / (2f * zoom * ppu);
    }
}