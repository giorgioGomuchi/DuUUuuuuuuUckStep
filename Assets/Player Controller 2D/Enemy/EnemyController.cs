using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
public class EnemyController : MonoBehaviour
{
    [Header("Data")]
    public EnemyDataSO data;

    [Header("References")]
    public Transform player;
    public Animator animator;
    [SerializeField] private SpriteRenderer spriteRenderer;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [HideInInspector] public Rigidbody2D rb;

    private float knockbackTimer;

    private static readonly int SPEED_HASH = Animator.StringToHash("Speed");
    private static readonly int HIT_HASH = Animator.StringToHash("Hit");
    private static readonly int DIE_HASH = Animator.StringToHash("Die");

    private float movementLockTimer;

    public bool IsMovementLocked => movementLockTimer > 0f;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();

        // Si el enemigo vuela no queremos gravedad
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        if (player == null)
            player = GameObject.FindGameObjectWithTag("Player")?.transform;

        if (spriteRenderer != null && data != null && data.sprite != null)
            spriteRenderer.sprite = data.sprite;
    }

    private void Update()
    {
        if (knockbackTimer > 0f)
        {
            knockbackTimer -= Time.deltaTime;

            if (animator != null)
                animator.SetFloat(SPEED_HASH, rb.velocity.magnitude);

            return;
        }

        if (movementLockTimer > 0f)
            movementLockTimer -= Time.deltaTime;

        if (animator != null)
            animator.SetFloat(SPEED_HASH, rb.velocity.magnitude);
    }

    // =========================
    // MOVEMENT
    // =========================

    public void Move(Vector2 direction, float speed)
    {

        if (IsMovementLocked)
        {
            rb.velocity = Vector2.zero;
            return;
        }
        rb.velocity = direction * speed;
        UpdateFacing(direction);
    }

    public void MoveTowards(Vector2 target, float speed)
    {
        Vector2 dir = (target - (Vector2)transform.position).normalized;
        rb.velocity = dir * speed;
        UpdateFacing(dir);
    }

    public void StopMovement()
    {
        rb.velocity = Vector2.zero;
    }

    private void UpdateFacing(Vector2 movement)
    {
        if (spriteRenderer == null) return;
        if (Mathf.Abs(movement.x) < 0.05f) return;

        spriteRenderer.flipX = movement.x < 0f;
    }

    // =========================
    // HELPERS
    // =========================

    public bool PlayerInRange(float range)
    {
        if (player == null) return false;
        return Vector2.Distance(transform.position, player.position) <= range;
    }

    public Vector2 DirectionToPlayer()
    {
        if (player == null) return Vector2.zero;
        return ((Vector2)player.position - (Vector2)transform.position).normalized;
    }

    // =========================
    // ANIMATION FEEDBACK
    // =========================

    public void PlayHit()
    {
        if (animator == null) return;
        animator.SetTrigger(HIT_HASH);
    }

    public void PlayDie()
    {
        if (animator == null) return;
        animator.SetTrigger(DIE_HASH);
    }

    // =========================
    // KNOCKBACK
    // =========================

    public void ApplyKnockback(Vector2 direction, float force, float duration)
    {
        knockbackTimer = duration;

        rb.velocity = Vector2.zero;
        rb.AddForce(direction.normalized * force, ForceMode2D.Impulse);
    }

    public void LockMovement(float duration)
    {
        movementLockTimer = duration;
    }

   

    public void DisableAIAndMovement()
    {
        enabled = false;
        StopMovement();

        if (rb != null)
            rb.simulated = false;
    }
}
