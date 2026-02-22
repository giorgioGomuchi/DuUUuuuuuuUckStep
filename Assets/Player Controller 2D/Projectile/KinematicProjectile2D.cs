using UnityEngine;

[RequireComponent(typeof(Rigidbody2D))]
[RequireComponent(typeof(Collider2D))]
public abstract class KinematicProjectile2D : MonoBehaviour
{
    [Header("Lifetime")]
    [SerializeField] protected float lifetime = 5f;

    protected Rigidbody2D rb;
    protected Collider2D col;

    protected Vector2 direction;
    protected float speed;
    protected int damage;

    // IMPORTANTE: máscara de objetivos actuales (Player o Enemy, cambia con parry)
    protected LayerMask targetLayerMask;

    //Referencia publica para cambiar direccion desde el melerController
    public Vector2 CurrentDirection => direction;

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
    }

    protected virtual void FixedUpdate()
    {
        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);
    }

   

    // Por si algún collider es trigger (ej: parry, zonas)
    protected virtual void OnTriggerEnter2D(Collider2D other)
    {
    

        OnHit(other);
    }

    protected bool IsInTargetMask(Collider2D other)
    {
        return (targetLayerMask.value & (1 << other.gameObject.layer)) != 0;
    }

    protected void RotateToDirection()
    {
        transform.right = direction;
    }

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

        Debug.Log("Target mask value runtime: " + targetLayerMask.value);
        Debug.Log("Enemy layer index: " + LayerMask.NameToLayer("Enemy"));
        Debug.Log("Expected mask: " + (1 << LayerMask.NameToLayer("Enemy")));
    }



    protected abstract void OnHit(Collider2D other);

   
}

/*
UNITY SETUP (Prefab):
- Rigidbody2D: Kinematic, Gravity 0, Freeze Z
- Collider2D: NO Trigger (recomendado para colisión real)
- Collision Detection: Continuous (si aparece)
*/
