using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Boomerang")]
public class BoomerangAttackModuleSO : AttackModuleSO
{
    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {

        Debug.Log("[BoomerangAttackModuleSO] Execute ENTER", weapon);

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
        Debug.Log("[BoomerangAttackModuleSO] Projectile instantiated.", go);

        BoomerangProjectile2D proj = go.GetComponent<BoomerangProjectile2D>();
        if (proj == null)
        {
            Debug.LogWarning("[BoomerangAttackModuleSO] Missing BoomerangProjectile2D on prefab.", go);
            weapon.UnlockAttack();
            return false;
        }

        PlayerReferences playerRefs = weapon.GetComponentInParent<PlayerReferences>(true);
        BoomerangSequenceBridge bridge =
            playerRefs != null &&
            playerRefs.Combat != null
                ? playerRefs.Combat.BoomerangSequence
                : null;

        Debug.Log($"[BoomerangAttackModuleSO] Bridge via PlayerReferences/Combat? {bridge != null}", weapon);

        if (bridge != null)
        {
            proj.SetSequenceBridge(bridge);
            bool started = bridge.BeginSequence(proj, weapon, boom);
            Debug.Log($"[BoomerangAttackModuleSO] BeginSequence returned {started}", weapon);
        }
        else
        {
            Debug.LogWarning("[BoomerangAttackModuleSO] No BoomerangSequenceBridge found via PlayerReferences/Combat.", weapon);
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
            outboundDistanceAfterDeflect: boom.outboundDistanceAfterDeflect,
            returnSteering: boom.returnSteering,
            reflectableDistance: boom.reflectableDistance,
            catchDistance: boom.catchDistance,
            driftDeceleration: boom.driftDeceleration,
            spinDegPerSec: boom.spinDegPerSec,
            reflectableColor: boom.reflectableColor,
            reflectableFlashDuration: boom.reflectableFlashDuration,
            timedReturnArcStrength: boom.timedReturnArcStrength,
            timedReturnCatchBias: boom.timedReturnCatchBias
        );


        Debug.Log($"[BoomerangAttackModuleSO] Bridge found robust search? {bridge != null}", weapon);

        if (bridge != null)
        {
            proj.SetSequenceBridge(bridge);
            bool started = bridge.BeginSequence(proj, weapon, boom);
            Debug.Log($"[BoomerangAttackModuleSO] BeginSequence returned {started}", weapon);
        }
        else
        {
            Debug.LogWarning("[BoomerangAttackModuleSO] No BoomerangSequenceBridge found anywhere in hierarchy.", weapon);
        }

        weapon.SetVisualVisible(false);

        proj.onFinished += _ =>
        {
            Debug.Log("[BoomerangAttackModuleSO] Projectile finished -> unlock attack", weapon);
            weapon.UnlockAttack();
            weapon.SetVisualVisible(true);
        };

        return true;
    }

    
}