using UnityEngine;

public class EnemyExplosiveProjectile : EnemyProjectile
{
    [Header("Explosion")]
    [SerializeField] private float explosionRadius = 2f;
    [SerializeField] private int explosionDamage = 3;
    [SerializeField] private float explosionForce = 0f;
    [SerializeField] private GameObject explosionPrefab;

    private bool exploded;

    protected override void OnEnable()
    {
        base.OnEnable();
        exploded = false;
    }

    public void ConfigureExplosion(float radius, int damage, float force, GameObject prefab)
    {
        explosionRadius = radius;
        explosionDamage = damage;
        explosionForce = force;
        explosionPrefab = prefab;
    }

    protected override void OnHit(Collider2D other)
    {
        DebugLayerInfo(other);

        // 1) Parry
        if ((parryMask.value & (1 << other.gameObject.layer)) != 0)
        {
            Vector2 forward = other.transform.right;
            Reflect(forward);
            return;
        }

        // 2) Mundo
        if ((worldMask.value & (1 << other.gameObject.layer)) != 0)
        {
            if (debugLogs) Debug.Log("[EnemyExplosiveProjectile] Hit world -> Kill", this);
            Kill(); // o Explode() si quieres que explote al impactar con pared
            return;
        }

        // 3) Solo afecta si es target válido
        if (!IsInTargetMask(other))
            return;

        // Daño directo por impacto
        if (reflected)
        {
            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null) enemy.TakeDamage(damage);
        }
        else
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null) player.TakeDamage(damage);
        }

        Explode();
    }

    protected override void OnLifeTimeEnded()
    {
        Explode();
    }

    private void Explode()
    {
        if (exploded) return;
        exploded = true;

        if (explosionPrefab != null)
            Instantiate(explosionPrefab, transform.position, Quaternion.identity);

        // ✅ IMPORTANTE: respeta targetLayerMask (reflected cambia esto)
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayerMask);

        if (debugLogs)
            Debug.Log($"[EnemyExplosiveProjectile] Explode pos={transform.position} hits={hits.Length} mask={targetLayerMask.value}", this);

        foreach (var hit in hits)
        {
            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(explosionDamage);

            if (explosionForce > 0f && hit.attachedRigidbody != null)
            {
                Vector2 dir = ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;
                hit.attachedRigidbody.AddForce(dir * explosionForce, ForceMode2D.Impulse);
            }
        }

        Kill();
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}