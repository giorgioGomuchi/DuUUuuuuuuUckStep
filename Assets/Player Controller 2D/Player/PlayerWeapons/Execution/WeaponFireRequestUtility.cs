using UnityEngine;

public static class WeaponFireRequestUtility
{
    public static bool ShouldFirePrimaryThisFrame(PlayerInputReader input, WeaponDataSO weaponData)
    {
        if (input == null || weaponData == null)
            return false;

        if (UsesContinuousInput(weaponData))
            return input.FirePrimaryHeld;

        return input.ConsumePrimaryFireRequest();
    }

    public static bool ShouldFireSecondaryThisFrame(PlayerInputReader input, WeaponDataSO weaponData)
    {
        if (input == null || weaponData == null)
            return false;

        if (UsesContinuousInput(weaponData))
            return input.FireSecondaryHeld;

        return input.ConsumeSecondaryFireRequest();
    }

    private static bool UsesContinuousInput(WeaponDataSO weaponData)
    {
        return weaponData != null &&
               weaponData.cadenceMode == WeaponCadenceMode.Continuous;
    }
}