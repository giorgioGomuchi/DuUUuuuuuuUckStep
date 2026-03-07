using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class PlayerMovement : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO config;

    private Rigidbody2D rb;
    private bool velocityOverride;

    public Vector2 CurrentVelocity => rb.velocity;
    public float MoveSpeed => config != null ? config.moveSpeed : 5f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
    }

    public void SetConfig(PlayerConfigSO playerConfig)
    {
        config = playerConfig;
    }

    public void Move(Vector2 direction)
    {
        if (velocityOverride) return;
        rb.velocity = direction * MoveSpeed;
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