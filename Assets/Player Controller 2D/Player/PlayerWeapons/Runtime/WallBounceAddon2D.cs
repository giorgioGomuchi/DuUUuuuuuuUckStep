using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class WallBounceAddon2D : MonoBehaviour, IBounceConfigurable
{
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private LayerMask wallMask;
    private int remainingBounces;
    private float speedMultiplierPerBounce = 0.85f;

    private KinematicProjectile2D projectile;
    private Collider2D projectileCollider;

    public void ConfigureBounce(LayerMask wallMask, int maxBounces, float speedMultiplierPerBounce)
    {
        this.wallMask = wallMask;
        this.remainingBounces = Mathf.Max(0, maxBounces);
        this.speedMultiplierPerBounce = Mathf.Clamp(speedMultiplierPerBounce, 0.1f, 1f);

        if (projectile == null)
            projectile = GetComponent<KinematicProjectile2D>();

        if (projectileCollider == null)
            projectileCollider = GetComponent<Collider2D>();

        if (debugLogs)
            Debug.Log($"[WallBounceAddon2D] Config wallMask={wallMask.value} bounces={remainingBounces} mult={this.speedMultiplierPerBounce}", this);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (remainingBounces <= 0) return;
        if (projectile == null) return;

        // Solo paredes
        if (((1 << other.gameObject.layer) & wallMask.value) == 0)
            return;

        // -------- CALCULAR NORMAL MANUALMENTE --------

        // Punto más cercano del muro al centro del proyectil
        Vector2 contactPoint = other.ClosestPoint(transform.position);

        // Normal aproximada = desde punto de contacto hacia el proyectil
        Vector2 normal = ((Vector2)transform.position - contactPoint).normalized;

        if (normal.sqrMagnitude < 0.0001f)
        {
            if (debugLogs)
                Debug.LogWarning("[WallBounceAddon2D] Normal too small, skipping bounce.", this);
            return;
        }

        Vector2 inDir = projectile.CurrentDirection.normalized;
        Vector2 outDir = Vector2.Reflect(inDir, normal).normalized;

        projectile.SetDirection(outDir, rotate: true);
        projectile.SetSpeed(projectile.GetSpeed() * speedMultiplierPerBounce);

        remainingBounces--;

        if (debugLogs)
            Debug.Log($"[WallBounceAddon2D] Bounce! remaining={remainingBounces} normal={normal} outDir={outDir}", this);
    }
}