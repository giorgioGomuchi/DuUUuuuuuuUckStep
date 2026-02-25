using UnityEngine;

public abstract class AttackModuleSO : ScriptableObject, IAttackModule
{
    public abstract void Execute(WeaponBehaviour weapon, WeaponDataSO data);
}