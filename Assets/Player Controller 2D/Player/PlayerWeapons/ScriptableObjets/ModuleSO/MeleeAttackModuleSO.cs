using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Melee")]
public class MeleeAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (data is not MeleeAnimatedWeaponDataSO meleeData)
        {
            Debug.LogError("[MeleeAttackModuleSO] Wrong WeaponData type.", weapon);
            return false;
        }

        if (meleeData.hitPrefab == null)
        {
            Debug.LogError("[MeleeAttackModuleSO] hitPrefab is null.", weapon);
            return false;
        }

        var go = Object.Instantiate(meleeData.hitPrefab, weapon.FirePoint.position, weapon.transform.rotation);
        var controller = go.GetComponent<MeleeHitController>();
        if (controller == null)
        {
            Debug.LogError("[MeleeAttackModuleSO] hitPrefab missing MeleeHitController.", weapon);
            return false;
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

        return true;
    }
}