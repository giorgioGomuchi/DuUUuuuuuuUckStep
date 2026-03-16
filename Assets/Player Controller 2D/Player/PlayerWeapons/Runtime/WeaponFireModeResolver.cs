public static class WeaponFireModeResolver
{
    public static FireInputMode Resolve(WeaponDataSO weaponData)
    {
        if (weaponData == null)
            return FireInputMode.SinglePress;

        return weaponData.PreferredFireInputMode;
    }
}