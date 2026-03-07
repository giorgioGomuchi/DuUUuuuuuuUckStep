public static class WeaponDataTypeUtility
{
    public static bool IsMelee(WeaponDataSO weaponData)
    {
        return weaponData is MeleeAnimatedWeaponDataSO;
    }

    public static bool IsRanged(WeaponDataSO weaponData)
    {
        return weaponData is RangedWeaponDataSO;
    }
}