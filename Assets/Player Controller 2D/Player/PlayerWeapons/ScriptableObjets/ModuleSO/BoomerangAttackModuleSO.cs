using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Boomerang")]
public class BoomerangAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (!weapon.TryLockAttack())
            return false;

        if (data is not BoomerangWeaponDataSO boom)
            return false;

        if (boom.projectilePrefab == null)
        {
            weapon.UnlockAttack();
            return false;
        }

        GameObject go = Instantiate(boom.projectilePrefab, weapon.FirePoint.position, Quaternion.identity);
        var proj = go.GetComponent<BoomerangProjectile2D>();

        if (proj == null)
        {
            weapon.UnlockAttack();
            return false;
        }

        int finalDamage = weapon.ConsumeFinalDamage(boom.damage);

        proj.Initialize(weapon.CurrentAim, boom.projectileSpeed, finalDamage, boom.targetLayer);

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

        return true; // 🔥 ahora sí disparó
    }
}