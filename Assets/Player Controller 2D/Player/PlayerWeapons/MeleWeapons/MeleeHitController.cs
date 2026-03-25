using UnityEngine;

public class MeleeHitController : MonoBehaviour
{
    private int damage;
    private LayerMask targetLayer;
    private bool initialized;

    [Header("Knockback")]
    private float knockbackForce;

    [Header("Deflect / Parry")]
    [SerializeField] private bool enableDeflect = true;
    [SerializeField, Range(-1f, 1f)] private float deflectDotThreshold = 0.0f;
    [SerializeField] private float deflectSpeedMultiplier = 1.0f;
    [SerializeField] private bool disableColliderAfterDeflect = true;

    [Header("Boomerang")]
    [SerializeField] private bool allowBoomerangDeflectOnTriggerStay = true;

    [Header("Boomerang Rhythm Integration")]
    [SerializeField] private bool useRhythmForBoomerangRebound = true;
    [SerializeField] private BoomerangMusicDirector boomerangMusic;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private Animator animator;
    private Collider2D hitCollider;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        hitCollider = GetComponent<Collider2D>();

        if (boomerangMusic == null)
            boomerangMusic = FindFirstObjectByType<BoomerangMusicDirector>();
    }

    public void Initialize(int damage, LayerMask targetLayer, float fallbackLifetime, float knockbackForce)
    {
        this.damage = damage;
        this.targetLayer = targetLayer;
        this.knockbackForce = knockbackForce;
        initialized = true;

        float lifetime = fallbackLifetime;

        if (animator != null &&
            animator.runtimeAnimatorController != null &&
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
        if (TryHandleDeflect(other, "Enter"))
            return;

        HandleNormalDamage(other);
    }

    private void OnTriggerStay2D(Collider2D other)
    {
        if (!allowBoomerangDeflectOnTriggerStay)
            return;

        if (TryHandleDeflect(other, "Stay"))
            return;
    }

    private bool TryHandleDeflect(Collider2D other, string phase)
    {
        if (!enableDeflect)
            return false;

        var deflectable = other.GetComponent<IDeflectable2D>();
        if (deflectable == null)
            deflectable = other.GetComponentInParent<IDeflectable2D>();

        if (deflectable == null)
        {
            if (debugLogs)
                Debug.Log($"[MeleeHitController] DeflectCheck {phase} other={other.name} -> no IDeflectable2D", this);

            return false;
        }

        if (!deflectable.CanBeDeflected)
        {
            if (debugLogs)
                Debug.Log($"[MeleeHitController] DeflectCheck {phase} other={other.name} -> CanBeDeflected=false", this);

            return false;
        }

        var boomerang = other.GetComponent<BoomerangProjectile2D>();
        if (boomerang == null)
            boomerang = other.GetComponentInParent<BoomerangProjectile2D>();

        Vector2 forward = transform.right;

        Vector2 incoming = Vector2.zero;
        var proj = other.GetComponent<KinematicProjectile2D>();
        if (proj == null)
            proj = other.GetComponentInParent<KinematicProjectile2D>();

        if (proj != null)
            incoming = proj.CurrentDirection;

        float dot = (incoming.sqrMagnitude > 0.0001f)
            ? Vector2.Dot(incoming.normalized, -forward)
            : 1f;

        if (debugLogs)
        {
            string boomerangInfo = boomerang != null
                ? $" | boomerangState={boomerang.FlightState} | reflectable={boomerang.IsReflectable}"
                : "";

            Debug.Log(
                $"[MeleeHitController] DeflectCheck {phase} other={other.name} dot={dot:F3} threshold={deflectDotThreshold:F3} forward={forward} incoming={incoming}{boomerangInfo}",
                this);
        }

        if (dot <= deflectDotThreshold)
        {
            if (debugLogs)
                Debug.Log($"[MeleeHitController] DeflectCheck {phase} BLOCKED by dot", this);

            return false;
        }

        // ---------------------------------------------------------
        // BOOMERANG CON RITMO
        // ---------------------------------------------------------
        if (useRhythmForBoomerangRebound && boomerang != null && boomerangMusic != null)
        {
            bool success = boomerangMusic.TryRegisterRebound();

            if (!success)
            {
                if (debugLogs)
                    Debug.Log($"[MeleeHitController] Boomerang rebound FAIL phase={phase} (outside beat window)", this);

                return false;
            }

            var infoRhythm = new DeflectInfo(
                newDirection: forward,
                newTargetMask: targetLayer,
                speedMultiplier: deflectSpeedMultiplier,
                instigator: this
            );

            deflectable.Deflect(infoRhythm);

            if (debugLogs)
                Debug.Log($"[MeleeHitController] Boomerang rebound SUCCESS phase={phase}", this);

            if (disableColliderAfterDeflect && hitCollider != null)
                hitCollider.enabled = false;

            return true;
        }

        // ---------------------------------------------------------
        // DEFLECT NORMAL
        // ---------------------------------------------------------
        var info = new DeflectInfo(
            newDirection: forward,
            newTargetMask: targetLayer,
            speedMultiplier: deflectSpeedMultiplier,
            instigator: this
        );

        deflectable.Deflect(info);

        if (debugLogs)
            Debug.Log($"[MeleeHitController] DEFLECT success phase={phase} other={other.name}", this);

        if (disableColliderAfterDeflect && hitCollider != null)
            hitCollider.enabled = false;

        return true;
    }

    private void HandleNormalDamage(Collider2D other)
    {
        if (((1 << other.gameObject.layer) & targetLayer.value) == 0)
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            if (debugLogs)
                Debug.Log($"[MeleeHitController] HIT other={other.name} dmg={damage}", this);
        }

        var enemyHealth = other.GetComponentInParent<EnemyHealth>();
        if (enemyHealth != null)
        {
            Vector2 hitDir = ((Vector2)other.transform.position - (Vector2)transform.position).normalized;
            enemyHealth.ApplyKnockback(hitDir, knockbackForce);

            if (debugLogs)
                Debug.Log($"[MeleeHitController] Knockback dir={hitDir} force={knockbackForce}", this);
        }

        if (hitCollider != null)
            hitCollider.enabled = false;
    }
}