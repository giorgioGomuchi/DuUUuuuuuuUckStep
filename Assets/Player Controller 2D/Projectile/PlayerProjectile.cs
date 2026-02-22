using UnityEngine;

public class PlayerProjectile : KinematicProjectile2D
{
    protected override void OnHit(Collider2D other)
    {
        if (!IsInTargetMask(other))
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damage);

        Kill();
    }
}
