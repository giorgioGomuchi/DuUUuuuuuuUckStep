using UnityEngine;

public class WeaponFireExecutor : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponSlotsController weaponSlots;
    [SerializeField] private RhythmCombatController rhythmCombat;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private IFireMode normalMode;
    private IFireMode rhythmMode;

    private void Awake()
    {
        if (weaponSlots == null)
            weaponSlots = GetComponent<WeaponSlotsController>();

        if (weaponSlots == null)
            weaponSlots = GetComponentInParent<WeaponSlotsController>();

        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();

        normalMode = new NormalFireMode();
        rhythmMode = new RhythmFireMode(rhythmCombat);
    }

    public bool FireSlot(WeaponSlotType slot)
    {
        if (weaponSlots == null)
            return false;

        WeaponBehaviour weapon = weaponSlots.GetWeaponBySlot(slot);
        if (weapon == null || weapon.WeaponData == null)
            return false;

        CombatAction action = BuildCombatAction(weapon);
        IFireMode fireMode = ResolveFireMode(weapon.WeaponData);

        bool didFire = fireMode.TryFire(weapon, action);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponFireExecutor] FireSlot={slot} Weapon={(weapon.WeaponData != null ? weapon.WeaponData.weaponName : "NULL")} " +
                $"Action={action} Result={didFire}",
                this);
        }

        return didFire;
    }

    private IFireMode ResolveFireMode(WeaponDataSO weaponData)
    {
        if (weaponData == null)
            return normalMode;

        if (weaponSlots != null &&
            weaponSlots.IsRhythmSystemEnabled &&
            weaponData.useRhythmGate)
        {
            return rhythmMode;
        }

        return normalMode;
    }

    private CombatAction BuildCombatAction(WeaponBehaviour weapon)
    {
        if (weapon != null && weapon.WeaponData != null && WeaponDataTypeUtility.IsMelee(weapon.WeaponData))
            return CombatAction.Melee;

        return CombatAction.Ranged;
    }
}