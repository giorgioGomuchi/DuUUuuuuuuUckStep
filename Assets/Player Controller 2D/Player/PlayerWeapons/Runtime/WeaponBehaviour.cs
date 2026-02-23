using UnityEngine;

[RequireComponent(typeof(Transform))]
public class WeaponBehaviour : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private Transform firePoint;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer weaponVisual;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Vector2 currentAim = Vector2.right;
    private float nextFireTime;

    private float pendingDamageMultiplier = 1f;

    public WeaponDataSO WeaponData => weaponData;
    public float Cooldown => weaponData != null ? weaponData.cooldown : 0f;

    private void Awake()
    {
        SetupVisual();
    }

    #region Setup

    private void SetupVisual()
    {
        if (weaponData == null)
        {
            Debug.LogError($"[{name}] WeaponData missing.", this);
            return;
        }

        if (weaponVisual == null)
            weaponVisual = GetComponentInChildren<SpriteRenderer>();

        if (weaponVisual == null)
        {
            GameObject visualGO = new GameObject("WeaponVisual");
            visualGO.transform.SetParent(transform);
            visualGO.transform.localPosition = Vector3.zero;
            visualGO.transform.localRotation = Quaternion.identity;

            weaponVisual = visualGO.AddComponent<SpriteRenderer>();
            weaponVisual.sortingLayerName = "Default";
            weaponVisual.sortingOrder = 11;
        }

        if (weaponVisual.sprite == null && weaponData.weaponIcon != null)
            weaponVisual.sprite = weaponData.weaponIcon;
    }

    #endregion

    #region Public API

    public void SetAim(Vector2 direction)
    {
        if (direction == Vector2.zero)
            return;

        currentAim = direction.normalized;

        float angle = Mathf.Atan2(currentAim.y, currentAim.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0, 0, angle);
    }

    public void TryFire()
    {
        if (weaponData == null || firePoint == null)
            return;

        // MELEE
        if (weaponData is MeleeAnimatedWeaponDataSO meleeData)
        {
            FireMelee(meleeData);
            return;
        }

        // RANGED cooldown
        if (Time.time < nextFireTime)
            return;

        // SHOTGUN (specialized ranged)
        if (weaponData is ShotgunWeaponDataSO shotgunData)
        {
            FireShotgun(shotgunData);
            nextFireTime = Time.time + weaponData.cooldown;
            return;
        }

        // NORMAL RANGED (incl explosive)
        if (weaponData is RangedWeaponDataSO rangedData)
        {
            FireRanged(rangedData);
            nextFireTime = Time.time + weaponData.cooldown;
        }
    }

    public void SetNextAttackDamageMultiplier(float multiplier)
    {
        pendingDamageMultiplier = Mathf.Max(0.01f, multiplier);
    }

    public void SetWeaponData(WeaponDataSO newData)
    {
        weaponData = newData;
        SetupVisual();
    }

    #endregion

    #region Melee

    private void FireMelee(MeleeAnimatedWeaponDataSO data)
    {
        if (data.hitPrefab == null)
        {
            Debug.LogError($"[{name}] Melee hitPrefab is null.", this);
            return;
        }

        var go = Instantiate(data.hitPrefab, firePoint.position, transform.rotation);
        var controller = go.GetComponent<MeleeHitController>();

        if (controller == null)
        {
            Debug.LogError($"[{name}] hitPrefab missing MeleeHitController.", this);
            return;
        }

        int finalDamage = ApplyAndConsumeMultiplier(data.damage);

        controller.Initialize(
            finalDamage,
            data.targetLayer,
            data.hitLifetime,
            data.knockbackForce
        );

        if (debugLogs)
            Debug.Log($"[WeaponBehaviour] Melee spawned dmg={finalDamage}", this);
    }

    #endregion

    #region Ranged

    private void FireRanged(RangedWeaponDataSO data)
    {
        int finalDamage = ApplyAndConsumeMultiplier(data.damage);

        GameObject proj = Instantiate(data.projectilePrefab, firePoint.position, Quaternion.identity);

        var projectile = proj.GetComponent<KinematicProjectile2D>();
        if (projectile == null)
        {
            Debug.LogError($"[{name}] Projectile missing KinematicProjectile2D.", this);
            return;
        }

        projectile.Initialize(
            currentAim,
            data.projectileSpeed,
            finalDamage,
            data.targetLayer
        );

        // EXPLOSIVE SUPPORT (kept intact)
        if (data is ExplosiveWeaponDataSO explosiveData)
        {
            var explosive = proj.GetComponent<ExplosiveProjectile>();
            if (explosive != null)
            {
                explosive.ConfigureExplosion(
                    explosiveData.explosionRadius,
                    explosiveData.explosionDamage,
                    explosiveData.explosionForce,
                    explosiveData.explosionPrefab
                );
            }
        }

        if (debugLogs)
            Debug.Log($"[WeaponBehaviour] Ranged fired dmg={finalDamage}", this);
    }

    #endregion

    #region Shotgun

    private void FireShotgun(ShotgunWeaponDataSO data)
    {
        int baseDamage = ApplyAndConsumeMultiplier(data.damage);

        for (int i = 0; i < Mathf.Max(1, data.pellets); i++)
        {
            Vector2 dir = GetShotgunDirection(data, i);
            SpawnProjectile(data, dir, baseDamage);
        }

        if (debugLogs)
            Debug.Log($"[WeaponBehaviour] Shotgun fired pellets={data.pellets} dmg={baseDamage}", this);
    }

    private Vector2 GetShotgunDirection(ShotgunWeaponDataSO data, int pelletIndex)
    {
        float halfSpread = data.spreadAngleDegrees * 0.5f;

        float offset;
        if (data.randomSpread || data.pellets <= 1)
        {
            offset = Random.Range(-halfSpread, halfSpread);
        }
        else
        {
            // Evenly distributed pellets from -halfSpread to +halfSpread
            float t = (data.pellets == 1) ? 0.5f : (float)pelletIndex / (data.pellets - 1);
            offset = Mathf.Lerp(-halfSpread, halfSpread, t);
        }

        float baseAngle = Mathf.Atan2(currentAim.y, currentAim.x) * Mathf.Rad2Deg;
        float finalAngle = baseAngle + offset;

        return new Vector2(
            Mathf.Cos(finalAngle * Mathf.Deg2Rad),
            Mathf.Sin(finalAngle * Mathf.Deg2Rad)
        ).normalized;
    }

    private void SpawnProjectile(RangedWeaponDataSO data, Vector2 direction, int damage)
    {
        GameObject proj = Instantiate(data.projectilePrefab, firePoint.position, Quaternion.identity);

        var projectile = proj.GetComponent<KinematicProjectile2D>();
        if (projectile == null)
        {
            Debug.LogError($"[{name}] Projectile missing KinematicProjectile2D.", this);
            return;
        }

        projectile.Initialize(direction, data.projectileSpeed, damage, data.targetLayer);

        // Explosive still supported
        if (data is ExplosiveWeaponDataSO explosiveData)
        {
            var explosive = proj.GetComponent<ExplosiveProjectile>();
            if (explosive != null)
            {
                explosive.ConfigureExplosion(
                    explosiveData.explosionRadius,
                    explosiveData.explosionDamage,
                    explosiveData.explosionForce,
                    explosiveData.explosionPrefab
                );
            }
        }
    }

    #endregion

    #region Utility

    private int ApplyAndConsumeMultiplier(int baseDamage)
    {
        int finalDamage = Mathf.RoundToInt(baseDamage * pendingDamageMultiplier);
        pendingDamageMultiplier = 1f;
        return finalDamage;
    }

    #endregion
}