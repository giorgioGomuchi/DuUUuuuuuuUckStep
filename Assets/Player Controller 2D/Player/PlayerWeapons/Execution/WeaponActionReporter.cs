using UnityEngine;

public class WeaponActionReporter : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponSlotsController weaponSlots;
    [SerializeField] private WeaponOverrideController weaponOverride;
    [SerializeField] private PlayerReferences playerReferences;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private void Awake()
    {
        if (weaponSlots == null)
            weaponSlots = GetComponent<WeaponSlotsController>();

        if (weaponSlots == null)
            weaponSlots = GetComponentInParent<WeaponSlotsController>();

        if (weaponOverride == null)
            weaponOverride = GetComponentInParent<WeaponOverrideController>();

        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();
    }

    public void ReportSuccessfulFire(WeaponSlotType slot)
    {
        if (weaponSlots == null)
            return;

        WeaponBehaviour weapon = weaponSlots.GetWeaponBySlot(slot);
        if (weapon == null || weapon.WeaponData == null)
            return;

        ConsumeOverrideAmmoIfNeeded(weapon);
        RecordCombatAction(weapon);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponActionReporter] ReportSuccessfulFire Slot={slot} Weapon={weapon.WeaponData.weaponName}",
                this);
        }
    }

    public void RecordSwitchWeaponAction(Vector2 actionDirection, Vector2 aimDirection)
    {
        if (playerReferences == null || playerReferences.ActionRecorder == null)
            return;

        PlayerActionData actionData = new PlayerActionData(
            PlayerActionType.SwitchWeapon,
            actionDirection,
            aimDirection,
            "SwitchWeapon"
        );

        playerReferences.ActionRecorder.RecordAction(actionData);

        if (debugLogs)
        {
            Debug.Log("[WeaponActionReporter] SwitchWeapon action recorded.", this);
        }
    }

    private void ConsumeOverrideAmmoIfNeeded(WeaponBehaviour weapon)
    {
        weaponOverride?.ConsumeAmmoIfNeeded(weapon);
    }

    private void RecordCombatAction(WeaponBehaviour weapon)
    {
        if (playerReferences == null || playerReferences.ActionRecorder == null)
            return;

        PlayerActionType actionType = ResolveActionType(weapon.WeaponData);

        PlayerActionData actionData = new PlayerActionData(
            actionType,
            weaponSlots.CurrentAim,
            weaponSlots.CurrentAim,
            weapon.WeaponName
        );

        playerReferences.ActionRecorder.RecordAction(actionData);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponActionReporter] Combat action recorded -> {actionData.ActionType} | Source={actionData.SourceId}",
                this);
        }
    }

    private PlayerActionType ResolveActionType(WeaponDataSO data)
    {
        if (data == null)
            return PlayerActionType.Ranged;

        if (WeaponDataTypeUtility.IsMelee(data))
            return PlayerActionType.Melee;

        return PlayerActionType.Ranged;
    }
}