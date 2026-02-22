using UnityEngine;

/// <summary>
/// Arma genérica para enemigos.
/// - Hace Aim (rotación pivot) igual que tu sniper antiguo.
/// - Dispara proyectiles usando Projectile.Initialize(direction, speed, targetLayer).
/// - Pasa por pipeline de WeaponModifiers (laser, spread, explosión, etc.).
/// 
/// IMPORTANTE:
/// - El Brain decide CUÁNDO y CUÁNTAS VECES disparar.
/// - El arma decide CÓMO spawnear/configurar cada proyectil.
/// </summary>
public class EnemyWeapon : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private EnemyWeaponDataSO weaponData;

    [Header("References")]
    [SerializeField] private Transform weaponPivot;
    [SerializeField] private Transform firePoint;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;

    public Transform FirePoint => firePoint;
    public Transform WeaponPivot => weaponPivot;
    public EnemyWeaponDataSO WeaponData => weaponData;

    /// <summary>
    /// Rota el pivot hacia la dirección (y hace flip con la técnica del eje Y).
    /// Llamar SIEMPRE antes de disparar si quieres que el arma “apunte”.
    /// </summary>
    public void Aim(Vector2 direction)
    {
        if (weaponPivot == null) return;
        if (direction == Vector2.zero) return;

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        weaponPivot.rotation = Quaternion.Euler(0f, 0f, angle);

        weaponPivot.localScale = direction.x < 0
            ? new Vector3(1f, -1f, 1f)
            : new Vector3(1f, 1f, 1f);
    }

    /// <summary>
    /// Disparo principal. Permite pasar shotIndex/shotCount para spread tipo sniper.
    /// </summary>
    public void Fire(Vector2 direction, int shotIndex = 0, int shotCount = 1)
    {
        if (weaponData == null || weaponData.projectilePrefab == null || firePoint == null)
            return;

        Vector2 baseDir = direction == Vector2.zero ? Vector2.right : direction.normalized;

        // Apunta SIEMPRE antes de disparar
        Aim(baseDir);

        WeaponFireContext ctx = new WeaponFireContext
        {
            weapon = this,
            baseDirection = baseDir,
            shotIndex = shotIndex,
            shotCount = Mathf.Max(1, shotCount),

            projectilePrefab = weaponData.projectilePrefab,
            firePoint = firePoint,

            speed = weaponData.projectileSpeed,
            targetLayer = weaponData.targetLayer,
            directions = null
        };

        ctx.ResetDirectionsToBase();

        // --- BEFORE FIRE (modifiers) ---
        if (weaponData.modifiers != null)
        {
            foreach (var mod in weaponData.modifiers)
                mod?.BeforeFire(ref ctx);
        }

        if (ctx.directions == null || ctx.directions.Count == 0)
        {
            if (debugLogs) Debug.Log("[EnemyWeapon] No directions to fire (ctx.directions empty).", this);
            return;
        }

        // --- SPAWN per direction ---
        for (int i = 0; i < ctx.directions.Count; i++)
        {
            Vector2 dir = ctx.directions[i].normalized;
            if (dir == Vector2.zero) continue;

            GameObject proj = Instantiate(
                ctx.projectilePrefab,
                ctx.firePoint.position,
                Quaternion.identity
            );

            // Tu firma actual: Initialize(Vector2 direction, float speed, LayerMask targetLayer)
            EnemyProjectile p = proj.GetComponent<EnemyProjectile>();
            if (p != null)
            {
                Debug.Log("---- SPAWN PROJECTILE ----");
                Debug.Log("TargetLayer from SO: " + weaponData.targetLayer.value);
                Debug.Log("TargetLayer name: " + LayerMask.LayerToName(Mathf.RoundToInt(Mathf.Log(weaponData.targetLayer.value, 2))));

                p.Initialize(dir, ctx.speed, weaponData.damage, ctx.targetLayer);
            }
            // --- AFTER SPAWN (modifiers) ---
            if (weaponData.modifiers != null)
            {
                foreach (var mod in weaponData.modifiers)
                    mod?.AfterSpawnedProjectile(ref ctx, proj);
            }
        }

        // --- AFTER FIRE (modifiers) ---
        if (weaponData.modifiers != null)
        {
            foreach (var mod in weaponData.modifiers)
                mod?.AfterFire(ref ctx);
        }
    }

    // =========================
    // UNITY SETUP (cómo configurarlo)
    // =========================
    // 1) Añade EnemyWeapon al root del enemigo (o donde tengas el brain).
    // 2) Asigna WeaponData (EnemyWeaponDataSO).
    // 3) Asigna WeaponPivot (Transform que rota).
    // 4) Asigna FirePoint (Transform desde donde sale el proyectil).
}
