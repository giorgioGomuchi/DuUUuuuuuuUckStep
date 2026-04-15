using UnityEngine;

public class PlayerProjectile : KinematicProjectile2D
{

    [SerializeField] private float spriteAngleOffset = -90f;
    protected override void OnHit(Collider2D other)
    {
        if (other == null)
            return;

        BoomerangProjectile2D boomerang = other.GetComponent<BoomerangProjectile2D>();
        if (boomerang == null)
            boomerang = other.GetComponentInParent<BoomerangProjectile2D>();

        if (boomerang != null)
        {
            Debug.Log("[PlayerProjectile] Hit boomerang.", this);

            BoomerangLoopController loop = FindFirstObjectByType<BoomerangLoopController>();
            if (loop != null)
            {
                Vector2 hitPoint = other.ClosestPoint(transform.position);
                if (loop.TryResolveRecallShotRedirect(this, boomerang, hitPoint))
                {
                    Kill();
                    return;
                }
            }
        }

        if (!IsInTargetMask(other))
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damage);

        Kill();
    }

    public override void Initialize(Vector2 dir, float projectileSpeed, int dmg, LayerMask targetMask)
    {
        base.Initialize(dir, projectileSpeed, dmg, targetMask);

        if (Mathf.Abs(spriteAngleOffset) > 0.001f)
            transform.rotation *= Quaternion.Euler(0f, 0f, spriteAngleOffset);
    }
}
