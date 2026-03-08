using UnityEngine;

[CreateAssetMenu(fileName = "WeaponOverrideComboEffect", menuName = "Game/Player/Combo Effects/Weapon Override")]
public class WeaponOverrideComboEffectSO : ComboEffectSO
{
    [Header("Override")]
    [SerializeField] private WeaponDataSO overrideWeaponData;
    [SerializeField] private WeaponSlotType targetSlot = WeaponSlotType.Main;
    [SerializeField] private int ammoCount = 2;

    public override void Apply(ComboEffectContext context)
    {
        if (context == null)
        {
            Debug.LogError("[WeaponOverrideComboEffectSO] Context is null.");
            return;
        }

        if (context.WeaponSlots == null)
        {
            Debug.LogError("[WeaponOverrideComboEffectSO] WeaponSlotsController is missing.");
            return;
        }

        if (overrideWeaponData == null)
        {
            Debug.LogError("[WeaponOverrideComboEffectSO] Override weapon data is missing.");
            return;
        }

        context.WeaponSlots.ApplyTemporaryWeaponOverride(
            targetSlot,
            overrideWeaponData,
            ammoCount);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponOverrideComboEffectSO] Applied override -> Slot={targetSlot}, Weapon={overrideWeaponData.weaponName}, Ammo={ammoCount}");
        }
    }
}