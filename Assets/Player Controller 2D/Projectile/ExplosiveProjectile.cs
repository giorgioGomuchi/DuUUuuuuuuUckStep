using UnityEngine;

public class ExplosiveProjectile : KinematicProjectile2D
{
    private float explosionRadius;
    private int explosionDamage;
    private float explosionForce;
    private GameObject explosionPrefab;

    private bool exploded;

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

    protected override void OnEnable()
    {
        base.OnEnable();
        exploded = false;
    }

    protected override void OnHit(Collider2D other)
    {
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

        Collider2D[] hits =
            Physics2D.OverlapCircleAll(transform.position, explosionRadius, targetLayerMask);
        
        
        Debug.Log("Explosion at: " + transform.position);
        Debug.Log("Hits found: " + hits.Length);

        foreach (var hit in hits)
        {
            var damageable = hit.GetComponentInParent<IDamageable>();
            if (damageable != null)
                damageable.TakeDamage(explosionDamage);

            if (explosionForce > 0f && hit.attachedRigidbody != null)
            {
                Vector2 dir =
                    ((Vector2)hit.transform.position - (Vector2)transform.position).normalized;

                hit.attachedRigidbody.AddForce(dir * explosionForce, ForceMode2D.Impulse);
            }
        }

        Kill();
    }
}
