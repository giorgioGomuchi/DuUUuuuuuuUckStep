using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Melee")]
public class MeleeAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override void Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (data is not MeleeAnimatedWeaponDataSO meleeData)
        {
            Debug.LogError("[MeleeAttackModuleSO] Wrong WeaponData type.", weapon);
            return;
        }

        if (meleeData.hitPrefab == null)
        {
            Debug.LogError("[MeleeAttackModuleSO] hitPrefab is null.", weapon);
            return;
        }

        var go = Object.Instantiate(meleeData.hitPrefab, weapon.FirePoint.position, weapon.transform.rotation);
        var controller = go.GetComponent<MeleeHitController>();
        if (controller == null)
        {
            Debug.LogError("[MeleeAttackModuleSO] hitPrefab missing MeleeHitController.", weapon);
            return;
        }

        int finalDamage = weapon.ConsumeFinalDamage(meleeData.damage);

        controller.Initialize(
            finalDamage,
            meleeData.targetLayer,
            meleeData.hitLifetime,
            meleeData.knockbackForce
        );

        if (debugLogs)
            Debug.Log($"[MeleeAttackModuleSO] Spawned hit dmg={finalDamage}", weapon);
    }
}