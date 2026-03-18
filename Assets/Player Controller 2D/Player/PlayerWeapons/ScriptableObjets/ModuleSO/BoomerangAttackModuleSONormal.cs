using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/BoomerangNormal")]
public class BoomerangAttackModuleSONormal : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (weapon == null || data == null)
            return false;

        if (!weapon.TryLockAttack())
            return false;

        if (data is not BoomerangWeaponDataSONormal boom)
        {
            if (debugLogs)
                Debug.LogWarning(
                    $"[BoomerangNormal] Wrong data type. Expected {nameof(BoomerangWeaponDataSONormal)}, got {data.GetType().Name}",
                    weapon);

            weapon.UnlockAttack();
            return false;
        }

        if (boom.projectilePrefab == null)
        {
            if (debugLogs)
                Debug.LogWarning("[BoomerangNormal] Missing projectile prefab.", weapon);

            weapon.UnlockAttack();
            return false;
        }

        GameObject go = Instantiate(boom.projectilePrefab, weapon.FirePoint.position, Quaternion.identity);
        BoomerangProjectile2DNormal proj = go.GetComponent<BoomerangProjectile2DNormal>();

        if (proj == null)
        {
            if (debugLogs)
                Debug.LogWarning("[BoomerangNormal] Projectile prefab does not contain BoomerangProjectile2DNormal.", go);

            weapon.UnlockAttack();
            return false;
        }

        int finalDamage = weapon.ConsumeFinalDamage(boom.damage);

        proj.Initialize(
            weapon.CurrentAim,
            boom.projectileSpeed,
            finalDamage,
            boom.targetLayer);

        proj.ConfigureBoomerang(
            owner: weapon.transform,
            outboundDistance: boom.outboundDistance,
            returnSpeedMultiplier: boom.returnSpeedMultiplier,
            deflectOnlyWhileReturning: boom.deflectOnlyWhileReturning,
            outboundDistanceAfterDeflect: boom.outboundDistanceAfterDeflect
        );

        weapon.SetVisualVisible(false);

        proj.onFinished += _ =>
        {
            weapon.UnlockAttack();
            weapon.SetVisualVisible(true);
        };

        if (debugLogs)
            Debug.Log("[BoomerangNormal] Fired successfully.", weapon);

        return true;
    }
}