using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Boomerang Loop")]
public class BoomerangLoopAttackModuleSO : AttackModuleSO
{
    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (weapon == null || data == null)
            return false;

        if (!weapon.TryLockAttack())
            return false;

        if (data is not BoomerangLoopWeaponDataSO boom)
        {
            weapon.UnlockAttack();
            return false;
        }

        if (boom.projectilePrefab == null)
        {
            weapon.UnlockAttack();
            return false;
        }

        PlayerReferences playerRefs = weapon.GetComponentInParent<PlayerReferences>(true);
        BoomerangLoopController loopController = playerRefs != null && playerRefs.Combat != null
            ? playerRefs.Combat.BoomerangLoop
            : null;

        if (loopController == null)
        {
            weapon.UnlockAttack();
            Debug.LogWarning("[BoomerangLoopAttackModuleSO] Missing BoomerangLoopController.", weapon);
            return false;
        }

        if (loopController.HasActiveProjectile)
        {
            weapon.UnlockAttack();
            return false;
        }

        GameObject go = Instantiate(boom.projectilePrefab, weapon.FirePoint.position, Quaternion.identity);
        BoomerangProjectile2D projectile = go.GetComponent<BoomerangProjectile2D>();

        if (projectile == null)
        {
            weapon.UnlockAttack();
            Destroy(go);
            Debug.LogWarning("[BoomerangLoopAttackModuleSO] Missing BoomerangProjectile2D on prefab.", weapon);
            return false;
        }

        int finalDamage = weapon.ConsumeFinalDamage(boom.damage);

        projectile.Initialize(
            weapon.CurrentAim,
            boom.projectileSpeed,
            finalDamage,
            boom.targetLayer);

        projectile.Configure(
            owner: loopController.CatchAnchor != null ? loopController.CatchAnchor : weapon.transform,
            config: boom.BuildProjectileConfig());

        projectile.SetSequenceBridge(loopController);

        if (!loopController.BeginLoop(projectile, weapon, boom))
        {
            weapon.UnlockAttack();
            Destroy(go);
            return false;
        }

        weapon.SetVisualVisible(false);

        projectile.onFinished += _ =>
        {
            weapon.UnlockAttack();
            weapon.SetVisualVisible(true);
        };

        return true;
    }
}