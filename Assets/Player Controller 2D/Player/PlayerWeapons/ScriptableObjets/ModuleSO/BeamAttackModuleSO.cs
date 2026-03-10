using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Beam")]
public class BeamAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (weapon == null || data == null)
            return false;

        if (data is not BeamWeaponDataSO)
        {
            Debug.LogError("[BeamAttackModuleSO] Wrong WeaponData type.", weapon);
            return false;
        }

        if (debugLogs)
            Debug.Log("[BeamAttackModuleSO] Beam weapon validated.", weapon);

        return true;
    }
}