using System;
using UnityEngine;

public class EnemyExplosiveProjectile : EnemyProjectile
{
    [Header("Collision")]
    [SerializeField] private LayerMask worldMask; // Walls/Obstacles/etc (para destruir si golpea mundo)
    [SerializeField] private LayerMask parryMask;

    private bool reflected;


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

    public void ConfigureExplosion(
       float radius,
       int damage,
       float force,
       GameObject prefab
   )
    {
        explosionRadius = radius;
        explosionDamage = damage;
        explosionForce = force;
        explosionPrefab = prefab;
    }

    protected override void OnHit(Collider2D other)
    {
        DebugLayerInfo(other);

        // 1) Parry (zonas/parry colliders)
        if ((parryMask.value & (1 << other.gameObject.layer)) != 0)
        {
            Vector2 forward = other.transform.right;
            Reflect(forward);
            return;
        }

        // 2) Mundo
        if ((worldMask.value & (1 << other.gameObject.layer)) != 0)
        {
            if (debugLogs) Debug.Log("[EnemyProjectile] Hit world -> Kill", this);
            Kill();
            return;
        }

        // 3) Daño normal
        if (!IsInTargetMask(other))
            return;

        if (reflected)
        {
            var enemy = other.GetComponentInParent<EnemyHealth>();
            if (enemy != null)
                enemy.TakeDamage(damage);
        }
        else
        {
            var player = other.GetComponentInParent<PlayerHealth>();
            if (player != null)
                player.TakeDamage(damage);
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

        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, explosionRadius);


        foreach (var hit in hits)
        {


            if (!IsInTargetMask(hit))
                continue;

            var damageable = hit.GetComponentInParent<IDamageable>();

            if (damageable != null)
                damageable.TakeDamage(explosionDamage);
        }

        Kill();
    }


    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, explosionRadius);
    }
}
