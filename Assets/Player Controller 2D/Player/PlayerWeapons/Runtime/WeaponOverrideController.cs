using System;
using UnityEngine;

public class WeaponOverrideController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponSlotsController weaponSlots;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool overrideActive;
    private WeaponSlotType overrideSlot;
    private WeaponDataSO cachedOverrideOriginalData;
    private WeaponDataSO activeOverrideData;
    private int overrideAmmoRemaining;

    public Action<WeaponSlotType> OnWeaponOverrideStarted;
    public Action<WeaponSlotType> OnWeaponOverrideEnded;

    public bool IsOverrideActive => overrideActive;
    public WeaponSlotType CurrentOverrideSlot => overrideSlot;
    public int OverrideAmmoRemaining => overrideAmmoRemaining;
    public WeaponDataSO ActiveOverrideData => activeOverrideData;

    private void Awake()
    {
        if (weaponSlots == null)
            weaponSlots = GetComponent<WeaponSlotsController>();

        if (weaponSlots == null)
            weaponSlots = GetComponentInParent<WeaponSlotsController>();
    }

    public void ApplyTemporaryWeaponOverride(WeaponSlotType slot, WeaponDataSO overrideWeaponData, int ammoCount)
    {
        if (weaponSlots == null)
        {
            Debug.LogError("[WeaponOverrideController] WeaponSlotsController missing.", this);
            return;
        }

        if (overrideWeaponData == null)
        {
            Debug.LogWarning("[WeaponOverrideController] Override data is null.", this);
            return;
        }

        if (ammoCount <= 0)
        {
            Debug.LogWarning("[WeaponOverrideController] ammoCount must be > 0.", this);
            return;
        }

        WeaponBehaviour targetWeapon = weaponSlots.GetWeaponBySlot(slot);
        if (targetWeapon == null || targetWeapon.WeaponData == null)
        {
            Debug.LogWarning($"[WeaponOverrideController] Target slot {slot} has no valid weapon.", this);
            return;
        }

        WeaponDataSO currentData = targetWeapon.WeaponData;

        bool currentIsRanged = WeaponDataTypeUtility.IsRanged(currentData);
        bool currentIsMelee = WeaponDataTypeUtility.IsMelee(currentData);

        bool overrideIsRanged = WeaponDataTypeUtility.IsRanged(overrideWeaponData);
        bool overrideIsMelee = WeaponDataTypeUtility.IsMelee(overrideWeaponData);

        bool compatibleType =
            (currentIsRanged && overrideIsRanged) ||
            (currentIsMelee && overrideIsMelee);

        if (!compatibleType)
        {
            Debug.LogWarning(
                $"[WeaponOverrideController] Override type mismatch. Slot={slot}, Current={currentData.GetType().Name}, Override={overrideWeaponData.GetType().Name}",
                this);
            return;
        }

        if (!overrideActive)
        {
            cachedOverrideOriginalData = currentData;
        }
        else if (overrideSlot != slot)
        {
            Debug.LogWarning("[WeaponOverrideController] Another override is already active on a different slot.", this);
            return;
        }

        overrideActive = true;
        overrideSlot = slot;
        activeOverrideData = overrideWeaponData;
        overrideAmmoRemaining = ammoCount;

        targetWeapon.SetWeaponData(overrideWeaponData);
        targetWeapon.SetAim(weaponSlots.CurrentAim);

        OnWeaponOverrideStarted?.Invoke(slot);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponOverrideController] Override ON | Slot={slot} | Weapon={overrideWeaponData.weaponName} | Ammo={overrideAmmoRemaining}",
                this);
        }
    }

    public void ConsumeAmmoIfNeeded(WeaponBehaviour firedWeapon)
    {
        if (!overrideActive || weaponSlots == null || firedWeapon == null)
            return;

        WeaponBehaviour overrideWeapon = weaponSlots.GetWeaponBySlot(overrideSlot);
        if (firedWeapon != overrideWeapon)
            return;

        overrideAmmoRemaining--;
        overrideAmmoRemaining = Mathf.Max(0, overrideAmmoRemaining);

        if (debugLogs)
            Debug.Log($"[WeaponOverrideController] Override ammo left: {overrideAmmoRemaining}", this);

        if (overrideAmmoRemaining == 0)
            EndWeaponOverride();
    }

    public void ClearActiveOverride()
    {
        EndWeaponOverride();
    }

    private void EndWeaponOverride()
    {
        if (!overrideActive || weaponSlots == null)
            return;

        WeaponBehaviour targetWeapon = weaponSlots.GetWeaponBySlot(overrideSlot);

        if (targetWeapon != null && cachedOverrideOriginalData != null)
        {
            targetWeapon.SetWeaponData(cachedOverrideOriginalData);
            targetWeapon.SetAim(weaponSlots.CurrentAim);
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponOverrideController] Override OFF | Slot={overrideSlot} | Restored={(cachedOverrideOriginalData != null ? cachedOverrideOriginalData.weaponName : "null")}",
                this);
        }

        WeaponSlotType endedSlot = overrideSlot;

        overrideActive = false;
        cachedOverrideOriginalData = null;
        activeOverrideData = null;
        overrideAmmoRemaining = 0;

        OnWeaponOverrideEnded?.Invoke(endedSlot);
    }
}