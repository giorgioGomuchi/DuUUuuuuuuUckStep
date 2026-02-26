using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Ranged")]
public class RangedAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (data is not RangedWeaponDataSO rangedData)
        {
            Debug.LogError("[RangedAttackModuleSO] Wrong WeaponData type.", weapon);
            return false;
        }

        if (rangedData.projectilePrefab == null)
        {
            Debug.LogError("[RangedAttackModuleSO] projectilePrefab is null.", weapon);
            return false;
        }

        int finalDamage = weapon.ConsumeFinalDamage(rangedData.damage);

        GameObject projGO = Object.Instantiate(rangedData.projectilePrefab, weapon.FirePoint.position, Quaternion.identity);
        var projectile = projGO.GetComponent<KinematicProjectile2D>();
        if (projectile == null)
        {
            Debug.LogError("[RangedAttackModuleSO] Projectile missing KinematicProjectile2D.", weapon);
            return false;
        }

        projectile.Initialize(weapon.CurrentAim, rangedData.projectileSpeed, finalDamage, rangedData.targetLayer);

        // Explosive support (idéntico a tu WeaponBehaviour)
        if (rangedData is ExplosiveWeaponDataSO explosiveData)
        {
            var explosive = projGO.GetComponent<ExplosiveProjectile>();
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
            Debug.Log($"[RangedAttackModuleSO] Fired dmg={finalDamage}", weapon);

        return true;

    }
}