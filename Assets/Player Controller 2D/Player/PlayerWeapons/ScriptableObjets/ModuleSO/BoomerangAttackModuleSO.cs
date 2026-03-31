using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Boomerang")]
public class BoomerangAttackModuleSO : AttackModuleSO
{
    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (weapon == null || data == null)
            return false;

        if (!weapon.TryLockAttack())
            return false;

        if (data is not BoomerangWeaponDataSO boom)
        {
            weapon.UnlockAttack();
            return false;
        }

        if (boom.projectilePrefab == null)
        {
            weapon.UnlockAttack();
            return false;
        }

        GameObject go = Instantiate(boom.projectilePrefab, weapon.FirePoint.position, Quaternion.identity);
        BoomerangProjectile2D projectile = go.GetComponent<BoomerangProjectile2D>();

        if (projectile == null)
        {
            Debug.LogWarning("[BoomerangAttackModuleSO] Missing BoomerangProjectile2D on prefab.", go);
            weapon.UnlockAttack();
            Destroy(go);
            return false;
        }

        PlayerReferences playerRefs = weapon.GetComponentInParent<PlayerReferences>(true);
        BoomerangSequenceController bridge = playerRefs != null && playerRefs.Combat != null
            ? playerRefs.Combat.BoomerangSequence
            : null;

        int finalDamage = weapon.ConsumeFinalDamage(boom.damage);

        projectile.Initialize(
            weapon.CurrentAim,
            boom.projectileSpeed,
            finalDamage,
            boom.targetLayer);

        projectile.Configure(
            owner: weapon.transform,
            config: boom.BuildProjectileConfig());

        if (bridge != null)
        {
            projectile.SetSequenceBridge(bridge);
            bridge.BeginSequence(projectile, weapon, boom);
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