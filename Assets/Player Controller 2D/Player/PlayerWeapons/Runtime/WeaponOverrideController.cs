using System;
using UnityEngine;

public class WeaponOverrideController : MonoBehaviour
{
    private enum OverrideLifetimeMode
    {
        None = 0,
        Ammo = 1,
        Duration = 2
    }

    [Header("Refs")]
    [SerializeField] private WeaponSlotsController weaponSlots;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private bool overrideActive;
    private WeaponSlotType overrideSlot;
    private WeaponDataSO cachedOverrideOriginalData;
    private WeaponDataSO activeOverrideData;

    private int overrideAmmoRemaining;
    private float overrideEndTime;
    private OverrideLifetimeMode lifetimeMode = OverrideLifetimeMode.None;

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

    private void Update()
    {
        if (!overrideActive || lifetimeMode != OverrideLifetimeMode.Duration)
            return;

        if (Time.time >= overrideEndTime)
            EndWeaponOverride();
    }

    public void ApplyTemporaryWeaponOverride(WeaponSlotType slot, WeaponDataSO overrideWeaponData, int ammoCount)
    {
        if (!ValidateCommon(slot, overrideWeaponData))
            return;

        if (ammoCount <= 0)
        {
            Debug.LogWarning("[WeaponOverrideController] ammoCount must be > 0.", this);
            return;
        }

        ActivateOverride(slot, overrideWeaponData);
        lifetimeMode = OverrideLifetimeMode.Ammo;
        overrideAmmoRemaining = ammoCount;
        overrideEndTime = 0f;

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponOverrideController] Override ON | Slot={slot} | Weapon={overrideWeaponData.weaponName} | Ammo={overrideAmmoRemaining}",
                this);
        }
    }

    public void ApplyTemporaryWeaponOverrideForDuration(WeaponSlotType slot, WeaponDataSO overrideWeaponData, float durationSeconds)
    {
        if (!ValidateCommon(slot, overrideWeaponData))
            return;

        if (durationSeconds <= 0f)
        {
            Debug.LogWarning("[WeaponOverrideController] durationSeconds must be > 0.", this);
            return;
        }

        ActivateOverride(slot, overrideWeaponData);
        lifetimeMode = OverrideLifetimeMode.Duration;
        overrideAmmoRemaining = 0;
        overrideEndTime = Time.time + durationSeconds;

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponOverrideController] Override ON | Slot={slot} | Weapon={overrideWeaponData.weaponName} | Duration={durationSeconds:F2}s",
                this);
        }
    }

    public void ConsumeAmmoIfNeeded(WeaponBehaviour firedWeapon)
    {
        if (!overrideActive || lifetimeMode != OverrideLifetimeMode.Ammo || weaponSlots == null || firedWeapon == null)
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

    private bool ValidateCommon(WeaponSlotType slot, WeaponDataSO overrideWeaponData)
    {
        if (weaponSlots == null)
        {
            Debug.LogError("[WeaponOverrideController] WeaponSlotsController missing.", this);
            return false;
        }

        if (overrideWeaponData == null)
        {
            Debug.LogWarning("[WeaponOverrideController] Override data is null.", this);
            return false;
        }

        WeaponBehaviour targetWeapon = weaponSlots.GetWeaponBySlot(slot);
        if (targetWeapon == null || targetWeapon.WeaponData == null)
        {
            Debug.LogWarning($"[WeaponOverrideController] Target slot {slot} has no valid weapon.", this);
            return false;
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
            return false;
        }

        if (!overrideActive)
        {
            cachedOverrideOriginalData = currentData;
        }
        else if (overrideSlot != slot)
        {
            Debug.LogWarning("[WeaponOverrideController] Another override is already active on a different slot.", this);
            return false;
        }

        return true;
    }

    private void ActivateOverride(WeaponSlotType slot, WeaponDataSO overrideWeaponData)
    {
        WeaponBehaviour targetWeapon = weaponSlots.GetWeaponBySlot(slot);

        overrideActive = true;
        overrideSlot = slot;
        activeOverrideData = overrideWeaponData;

        targetWeapon.SetWeaponData(overrideWeaponData);
        targetWeapon.SetAim(weaponSlots.CurrentAim);

        OnWeaponOverrideStarted?.Invoke(slot);
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
        overrideEndTime = 0f;
        lifetimeMode = OverrideLifetimeMode.None;

        OnWeaponOverrideEnded?.Invoke(endedSlot);
    }
}