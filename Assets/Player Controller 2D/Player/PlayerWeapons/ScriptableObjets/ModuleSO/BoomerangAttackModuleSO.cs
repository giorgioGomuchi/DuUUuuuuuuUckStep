using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Boomerang")]
public class BoomerangAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override void Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (data is not BoomerangWeaponDataSO boom)
        {
            Debug.LogError("[BoomerangAttackModuleSO] Wrong WeaponData type.", weapon);
            return;
        }

        if (boom.projectilePrefab == null)
        {
            Debug.LogError("[BoomerangAttackModuleSO] projectilePrefab is null.", weapon);
            return;
        }
        if (!weapon.TryLockAttack())
        {
            // Ya hay boomerang activo
            return;
        }

        int finalDamage = weapon.ConsumeFinalDamage(boom.damage);

        GameObject go = Object.Instantiate(boom.projectilePrefab, weapon.FirePoint.position, Quaternion.identity);
        var proj = go.GetComponent<BoomerangProjectile2D>();

        if (proj == null)
        {
            weapon.UnlockAttack();
            weapon.SetVisualVisible(true);
            Debug.LogError("[BoomerangAttackModuleSO] Prefab missing BoomerangProjectile2D.", weapon);
            return;
        }

        // Inicializa base projectile data
        proj.Initialize(weapon.CurrentAim, boom.projectileSpeed, finalDamage, boom.targetLayer);

        weapon.SetVisualVisible(false);

        weapon.SetVisualVisible(false);

        proj.onFinished += _ =>
        {
            weapon.UnlockAttack();
            weapon.SetVisualVisible(true);
        };

        proj.onReturnedToOwner += _ =>
        {
            weapon.SetVisualVisible(true);
        };

        // Configura boomerang specifics
        proj.ConfigureBoomerang(
            owner: weapon.transform,
            outboundDistance: boom.outboundDistance,
            returnSpeedMultiplier: boom.returnSpeedMultiplier,
            deflectOnlyWhileReturning: boom.deflectOnlyWhileReturning,
            outboundDistanceAfterDeflect: boom.outboundDistanceAfterDeflect
        );

        if (debugLogs)
            Debug.Log($"[BoomerangAttackModuleSO] Fired dmg={finalDamage}", weapon);
    }
}