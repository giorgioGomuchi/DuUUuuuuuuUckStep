using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [SerializeField] private float speed = 5f;

    private Rigidbody2D rb;
    private bool velocityOverride;

    public Vector2 CurrentVelocity => rb.velocity;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void Move(Vector2 direction)
    {
        if (velocityOverride) return;
        rb.velocity = direction * speed;
    }

    public void ForceVelocity(Vector2 velocity)
    {
        velocityOverride = true;
        rb.velocity = velocity;
    }

    public void ReleaseVelocityOverride()
    {
        velocityOverride = false;
    }

    public void Stop()
    {
        rb.velocity = Vector2.zero;
    }
}