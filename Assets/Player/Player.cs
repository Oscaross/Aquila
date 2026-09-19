using UnityEngine;

public class Player : MonoBehaviour
{
    [SerializeField] private Animator animator;
    public Animator Animator => animator;
    public Rigidbody2D Rigidbody { get; private set; }

    private Direction facing = Direction.Left;

    private void Awake()
    {
        Rigidbody = GetComponent<Rigidbody2D>();
    }

    public void SetFacing(Direction faceDir)
    {
        if (faceDir.Sign() == 0) return;
        facing = faceDir;
    }

    private void LateUpdate()
    {
        Vector3 scale = transform.localScale;
        scale.x = Mathf.Abs(scale.x) * -facing.Sign();
        transform.localScale = scale;
    }
}
