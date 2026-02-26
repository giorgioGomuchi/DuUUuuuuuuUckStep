using UnityEngine;

public abstract class AttackModuleSO : ScriptableObject, IAttackModule
{
    public abstract bool Execute(WeaponBehaviour weapon, WeaponDataSO data);
}