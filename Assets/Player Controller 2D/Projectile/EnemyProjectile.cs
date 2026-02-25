using UnityEngine;

public class EnemyProjectile : KinematicProjectile2D
{
    [Header("Collision")]
    [SerializeField] private LayerMask worldMask; // Walls/Obstacles/etc (para destruir si golpea mundo)
    [SerializeField] private LayerMask parryMask;

    private bool reflected;

    public void Reflect(Vector2 newDirection)
    {
        // Compatibilidad con tu API actual
        var info = new DeflectInfo(
            newDirection: newDirection,
            newTargetMask: 1 << LayerMask.NameToLayer("Enemy"),
            speedMultiplier: 1f,
            instigator: this
        );
        Deflect(info);
    }

    protected override void OnDeflected(DeflectInfo info)
    {
        reflected = true;

        if (debugLogs)
            Debug.Log("[EnemyProjectile] REFLECTED/DEFLECTED -> now targets Enemy", this);
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

        Kill();
    }
}