using UnityEngine;

public class EnemySniperWeapon : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform firePoint;
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Projectile")]
    [SerializeField] private GameObject projectilePrefab;
    [SerializeField] private float projectileSpeed;
    [SerializeField] private int damage;
    [SerializeField] private LayerMask targetLayer;

    public void Fire(Vector2 direction)
    {
        if (projectilePrefab == null || firePoint == null)
            return;

        GameObject proj = Instantiate(
            projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        EnemyProjectile p = proj.GetComponent<EnemyProjectile>();
        if (p != null)
            p.Initialize(direction, projectileSpeed, damage, targetLayer);
    }

    public void Aim(Vector2 direction)
    {
        if (weaponPivot == null) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        weaponPivot.localScale = direction.x < 0
            ? new Vector3(1f, -1f, 1f)
            : new Vector3(1f, 1f, 1f);
    }

    public void ResetIdlePose()
    {
        if (weaponPivot == null) return;

        bool facingLeft = weaponPivot.localScale.y < 0f;

        weaponPivot.rotation = facingLeft
            ? Quaternion.Euler(0f, 0f, 180f)
            : Quaternion.Euler(0f, 0f, 0f);
    }

    public void EnableLaser(bool enabled)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = enabled;
    }

    public void UpdateLaser(Vector3 from, Vector3 to)
    {
        if (lineRenderer == null) return;

        lineRenderer.SetPosition(0, from);
        lineRenderer.SetPosition(1, to);
    }

    public Transform FirePoint => firePoint;
}
