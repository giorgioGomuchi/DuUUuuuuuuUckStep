using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class KinematicProjectile2D : MonoBehaviour, IDeflectable2D
{
    [Header("Lifetime")]
    [SerializeField] protected float lifetime = 5f;

    [Header("Debug")]
    [SerializeField] protected bool debugLogs = false;

    protected Rigidbody2D rb;
    protected Collider2D col;

    protected Vector2 direction;
    protected float speed;
    protected int damage;

    // IMPORTANTE: máscara de objetivos actuales (Player o Enemy, cambia con parry/deflect)
    protected LayerMask targetLayerMask;

    // Expuesto para que otros scripts (melee/parry/bounce) puedan leerlo
    public Vector2 CurrentDirection => direction;

    // IDeflectable2D
    public virtual bool CanBeDeflected => true;

    protected virtual void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();

        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
        rb.freezeRotation = true;

        // Necesario para que Unity genere contactos entre kinematic-kinematic (bala vs bala)
        rb.useFullKinematicContacts = true;
    }

    protected virtual void OnEnable()
    {
        CancelInvoke(nameof(OnLifeTimeEnded));
        Invoke(nameof(OnLifeTimeEnded), lifetime);
    }

    public virtual void Initialize(Vector2 dir, float projectileSpeed, int dmg, LayerMask targetMask)
    {
        direction = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
        speed = projectileSpeed;
        damage = dmg;
        targetLayerMask = targetMask;

        RotateToDirection();

        if (debugLogs)
            Debug.Log($"[KinematicProjectile2D] Init dir={direction} speed={speed} dmg={damage} mask={targetLayerMask.value}", this);
    }

    protected virtual void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

    // --- HIT PIPELINE ---
    // Soporta triggers (parry zones, etc)
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
        OnHit(other);
    }

    // Soporta colisiones físicas (paredes, rebotes, etc)
    protected virtual void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision == null || collision.collider == null) return;
        OnHit(collision.collider);
    }

    protected bool IsInTargetMask(Collider2D other)
    {
        return (targetLayerMask.value & (1 << other.gameObject.layer)) != 0;
    }

    protected void RotateToDirection()
    {
        if (direction.sqrMagnitude > 0.0001f)
            transform.right = direction;
    }

    // --- PUBLIC HELPERS (para addons) ---
    public float GetSpeed() => speed;

    public void SetSpeed(float newSpeed)
    {
        speed = Mathf.Max(0f, newSpeed);
        if (debugLogs) Debug.Log($"[KinematicProjectile2D] SetSpeed -> {speed}", this);
    }

    public void SetDirection(Vector2 newDir, bool rotate = true)
    {
        if (newDir.sqrMagnitude < 0.0001f) return;
        direction = newDir.normalized;
        if (rotate) RotateToDirection();
        if (debugLogs) Debug.Log($"[KinematicProjectile2D] SetDirection -> {direction}", this);
    }

    // --- DEFLECT UNIFICADO ---
    public virtual void Deflect(DeflectInfo info)
    {
        if (!CanBeDeflected) return;

        SetDirection(info.newDirection, rotate: true);
        SetSpeed(speed * info.speedMultiplier);
        targetLayerMask = info.newTargetMask;

        OnDeflected(info);

        if (debugLogs)
        {
            Debug.Log($"[KinematicProjectile2D] Deflected by={info.instigator} dir={direction} speed={speed} newMask={targetLayerMask.value}", this);
        }
    }

    // Hook para que subclases (EnemyProjectile) mantengan su “reflected”
    protected virtual void OnDeflected(DeflectInfo info) { }

    protected virtual void Kill()
    {
        Destroy(gameObject);
    }

    protected virtual void OnLifeTimeEnded()
    {
        Kill();
    }

    protected void DebugLayerInfo(Collider2D other)
    {
        Debug.Log("----- LAYER DEBUG -----");
        Debug.Log("Other name: " + other.name);
        Debug.Log("Other layer index: " + other.gameObject.layer);
        Debug.Log("Other layer name: " + LayerMask.LayerToName(other.gameObject.layer));
        Debug.Log("Target mask value: " + targetLayerMask.value);
        Debug.Log("IsInTargetMask: " + IsInTargetMask(other));
        Debug.Log("Enemy layer index: " + LayerMask.NameToLayer("Enemy"));
    }

    protected abstract void OnHit(Collider2D other);
}

/*
UNITY SETUP (Prefab):
- Rigidbody2D: Kinematic, Gravity 0, Freeze Z
- Collider2D:
  - Si quieres rebotes con paredes: NO Trigger (y usa OnCollisionEnter2D)
  - Si quieres detecciones suaves/parry zones: Trigger (OnTriggerEnter2D)
*/