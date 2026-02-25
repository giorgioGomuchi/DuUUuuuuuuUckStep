using UnityEngine;

public class MeleeHitController : MonoBehaviour
{
    private int damage;
    private LayerMask targetLayer;
    private bool initialized;

    [Header("Knockback")]
    private float knockbackForce;

    [Header("Deflect/Parry")]
    [SerializeField] private bool enableDeflect = true;
    [SerializeField, Range(-1f, 1f)] private float deflectDotThreshold = 0.5f;
    [SerializeField] private float deflectSpeedMultiplier = 1.0f;
    [SerializeField] private bool disableColliderAfterDeflect = true;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Animator animator;
    private Collider2D hitCollider;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        hitCollider = GetComponent<Collider2D>();
    }

    public void Initialize(int damage, LayerMask targetLayer, float fallbackLifetime, float knockbackForce)
    {
        this.damage = damage;
        this.targetLayer = targetLayer;
        initialized = true;
        this.knockbackForce = knockbackForce;

        float lifetime = fallbackLifetime;

        if (animator != null && animator.runtimeAnimatorController != null &&
            animator.runtimeAnimatorController.animationClips.Length > 0)
        {
            lifetime = animator.runtimeAnimatorController.animationClips[0].length;
        }

        Destroy(gameObject, lifetime);

        if (debugLogs)
            Debug.Log($"[MeleeHitController] Init dmg={damage} targetMask={targetLayer.value} lifetime={lifetime}", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        // 0) DEFLECT primero, sin depender del targetLayer (para poder devolver balas enemigas)
        if (enableDeflect)
        {
            var deflectable = other.GetComponent<IDeflectable2D>();
            if (deflectable == null) deflectable = other.GetComponentInParent<IDeflectable2D>();

            if (deflectable != null && deflectable.CanBeDeflected)
            {
                Vector2 incoming = Vector2.zero;
                var proj = other.GetComponent<KinematicProjectile2D>();
                if (proj == null) proj = other.GetComponentInParent<KinematicProjectile2D>();
                if (proj != null) incoming = proj.CurrentDirection;

                Vector2 forward = transform.right;

                // Si no podemos leer incoming, permitimos deflect igualmente (más permisivo)
                float dot = (incoming.sqrMagnitude > 0.0001f) ? Vector2.Dot(incoming.normalized, -forward) : 1f;

                if (dot > deflectDotThreshold)
                {
                    var info = new DeflectInfo(
                        newDirection: forward,
                        newTargetMask: targetLayer,      // el melee “pertenece” al jugador => targetLayer suele ser Enemy
                        speedMultiplier: deflectSpeedMultiplier,
                        instigator: this
                    );

                    deflectable.Deflect(info);

                    if (debugLogs)
                        Debug.Log($"[MeleeHitController] DEFLECT success other={other.name} dot={dot}", this);

                    if (disableColliderAfterDeflect && hitCollider != null)
                        hitCollider.enabled = false;

                    return; // si deflecta, no hacemos daño a nada más en este trigger
                }
                else
                {
                    if (debugLogs)
                        Debug.Log($"[MeleeHitController] DEFLECT blocked by dot threshold dot={dot} other={other.name}", this);
                }
            }
        }

        // 1) Daño normal solo si está dentro del targetLayer
        if (((1 << other.gameObject.layer) & targetLayer.value) == 0) return;

        // daño
        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);
        }

        if (debugLogs)
            Debug.Log($"[MeleeHitController] HIT other={other.name} damageable={damageable}", this);

        // knockback
        var enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            Vector2 hitDir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
            enemyHealth.ApplyKnockback(hitDir, knockbackForce);

            if (debugLogs)
                Debug.Log($"[MeleeHitController] Knockback hitDir={hitDir} force={knockbackForce}", this);
        }

        // evitar multi-hit en el mismo swing
        if (hitCollider != null) hitCollider.enabled = false;
    }
}