using System.Collections.Generic;
using UnityEngine;

public class EnemyProjectile : KinematicProjectile2D
{
    [Header("Collision")]
    [SerializeField] private LayerMask worldMask; // Walls/Obstacles/etc (para destruir si golpea mundo)

    [SerializeField] private LayerMask parryMask;


    private bool reflected;

    public void Reflect(Vector2 newDirection)
    {
        reflected = true;

        direction = newDirection.normalized;
        RotateToDirection();

        // MUY IMPORTANTE: cambiar el mismo mask que usa IsInTargetMask
        targetLayerMask = 1 << LayerMask.NameToLayer("Enemy");


        Debug.Log("REFLECTED");
        Debug.Log("New mask value: " + targetLayerMask.value);
        Debug.Log("Enemy layer index: " + LayerMask.NameToLayer("Enemy"));


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

    protected bool IsWorldLayer(Collider2D other)
    {
        return (worldMask.value & (1 << other.gameObject.layer)) != 0;
    }

    protected bool IsParryLayer(Collider2D other)
    {
        return (parryMask.value & (1 << other.gameObject.layer)) != 0;
    }


}
