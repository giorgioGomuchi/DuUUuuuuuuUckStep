using UnityEngine;

public static class WeaponDataTypeUtility
{
    public static bool IsMelee(WeaponDataSO data)
    {
        return data is MeleeWeaponDataSO || data is MeleeAnimatedWeaponDataSO;
    }

    public static bool IsRanged(WeaponDataSO data)
    {
        return data is RangedWeaponDataSO
            || data is HitscanWeaponDataSO
            || data is BeamWeaponDataSO;
    }
}