public static class WeaponActionTypeResolver
{
    public static PlayerActionType Resolve(WeaponDataSO weaponData)
    {
        if (weaponData == null)
            return PlayerActionType.None;

        if (weaponData is MeleeAnimatedWeaponDataSO)
            return PlayerActionType.Melee;

        if (weaponData is RangedWeaponDataSO)
            return PlayerActionType.Ranged;

        return PlayerActionType.None;
    }

    public static bool IsRanged(WeaponDataSO weaponData)
    {
        return weaponData is RangedWeaponDataSO;
    }
}