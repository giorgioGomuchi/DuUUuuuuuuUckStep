using System;
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
        // 1️⃣ Parry
        if (IsParryLayer(other))
        {
            Vector2 forward = other.transform.right;
            Reflect(forward);
            return;
        }

        // 2️⃣ Mundo
        if (IsWorldLayer(other))
        {
            Explode();
            return;
        }

        // 3️⃣ Si impacta directamente un target válido
        if (IsInTargetMask(other))
        {
            var damageable = other.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(damage);
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
