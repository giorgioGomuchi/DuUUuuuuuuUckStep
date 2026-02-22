using UnityEngine;

[RequireComponent(typeof(Transform))]
public class WeaponBehaviour : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private WeaponDataSO weaponData;
    [SerializeField] private Transform firePoint;

    [Header("Visual")]
    [SerializeField] private SpriteRenderer weaponVisual;

    private Vector2 currentAim = Vector2.right;
    private float nextFireTime;

    public float Cooldown => weaponData.cooldown;

    private void Awake()
    {
        SetupVisual();
    }

    private void SetupVisual()
    {
        if (weaponData == null)
        {
            Debug.LogError($"[{name}] WeaponData missing.");
            return;
        }

        // Si no hay referencia al SpriteRenderer, lo buscamos
        if (weaponVisual == null)
        {
            weaponVisual = GetComponentInChildren<SpriteRenderer>();
        }

        // Si sigue sin existir, lo creamos
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

        // Si no tiene sprite asignado, usamos el del WeaponData
        if (weaponVisual.sprite == null && weaponData.weaponIcon != null)
        {
            weaponVisual.sprite = weaponData.weaponIcon;
        }
    }

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
        if (Time.time < nextFireTime)
            return;

        Fire();

        nextFireTime = Time.time + weaponData.cooldown;
    }

    private void Fire()
    {
        switch (weaponData)
        {
            case RangedWeaponDataSO ranged:
                FireRanged(ranged);
                break;

            case MeleeAnimatedWeaponDataSO melee:
                FireMelee(melee);
                break;
        }
    }

    private void FireRanged(RangedWeaponDataSO data)
    {
        GameObject proj = Instantiate(
            data.projectilePrefab,
            firePoint.position,
            Quaternion.identity
        );

        var projectile = proj.GetComponent<KinematicProjectile2D>();
        projectile.Initialize(
            currentAim,
            data.projectileSpeed,
            data.damage,
            data.targetLayer
        );

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

    private void FireMelee(MeleeAnimatedWeaponDataSO data)
    {
        var controller = Instantiate(data.hitPrefab, firePoint.position, transform.rotation).GetComponent<MeleeHitController>();

        controller.Initialize(
            data.damage,
            data.targetLayer,
            data.hitLifetime,
            data.knockbackForce
        );
    }
}
