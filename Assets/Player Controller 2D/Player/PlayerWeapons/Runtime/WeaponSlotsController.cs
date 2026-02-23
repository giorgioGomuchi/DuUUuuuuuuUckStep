using System.Collections;
using UnityEngine;

public class WeaponSlotsController : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private WeaponBehaviour mainWeapon;
    [SerializeField] private WeaponBehaviour secondaryWeapon;
    [SerializeField] private bool allowDualWield;

    [Header("Rhythm Combat")]
    [SerializeField] private RhythmCombatController rhythmCombat;
    [SerializeField] private float perfectDamageMultiplier = 1.75f;
    [SerializeField] private bool cancelAttackOnFail = true;

    [Header("Temporary Overrides")]
    [SerializeField] private bool debugLogs = true;

    private IWeaponState currentState;
    private SingleWieldState singleState;
    private DualWieldState dualState;

    private Vector2 currentAim;

    // Override state (we will override ONLY ranged weapons by default)
    private Coroutine rangedOverrideRoutine;
    private WeaponDataSO cachedMainWeaponData;
    private WeaponDataSO cachedSecondaryWeaponData;
    private bool hasCachedData;

    private void Awake()
    {
        singleState = new SingleWieldState(this);
        dualState = new DualWieldState(this);

        if (allowDualWield)
            currentState = dualState;
        else
            currentState = singleState;

        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();

        currentState.Enter(); // critical
    }

    #region State Control

    public void SetSingleWield()
    {
        currentState?.Exit();
        currentState = singleState;
        currentState.Enter();
    }

    public void SetDualWield()
    {
        currentState?.Exit();
        currentState = dualState;
        currentState.Enter();
    }

    #endregion

    #region Public API (called from PlayerRoot)

    public void FirePrimary() => currentState.FirePrimary();
    public void FireSecondary() => currentState.FireSecondary();
    public void SwitchWeapon() => currentState.SwitchWeapon();

    public void SetAim(Vector2 direction)
    {
        currentAim = direction;

        mainWeapon?.SetAim(direction);
        secondaryWeapon?.SetAim(direction);
    }

    #endregion

    #region Internal helpers used by states

    public void FireMain() => TryFireWithRhythm(mainWeapon);
    public void FireSecondaryWeapon() => TryFireWithRhythm(secondaryWeapon);

    public void SwapWeapons()
    {
        var temp = mainWeapon;
        mainWeapon = secondaryWeapon;
        secondaryWeapon = temp;

        SetAim(currentAim);
    }

    public void ShowMainOnly()
    {
        if (mainWeapon) mainWeapon.gameObject.SetActive(true);
        if (secondaryWeapon) secondaryWeapon.gameObject.SetActive(false);
    }

    public void ShowBoth()
    {
        if (mainWeapon) mainWeapon.gameObject.SetActive(true);
        if (secondaryWeapon) secondaryWeapon.gameObject.SetActive(true);
    }

    #endregion

    #region Rhythm Gate

    private void TryFireWithRhythm(WeaponBehaviour weapon)
    {
        if (weapon == null) return;

        CombatAction action = GetActionForWeapon(weapon);

        RhythmHitQuality quality = RhythmHitQuality.Good;
        if (rhythmCombat != null)
            quality = rhythmCombat.RegisterAttack(action);

        if (cancelAttackOnFail && quality == RhythmHitQuality.Fail)
        {
            if (debugLogs) Debug.Log($"[WeaponSlots] Cancel attack (Fail). action={action}", this);
            return;
        }

        if (quality == RhythmHitQuality.Perfect)
        {
            weapon.SetNextAttackDamageMultiplier(perfectDamageMultiplier);

            if (debugLogs) Debug.Log($"[WeaponSlots] Perfect! Applied dmg x{perfectDamageMultiplier:0.00} to {weapon.name}", this);
        }

        weapon.TryFire();
    }

    private CombatAction GetActionForWeapon(WeaponBehaviour weapon)
    {
        if (weapon == null) return CombatAction.Ranged;

        var data = weapon.WeaponData;
        return data is MeleeAnimatedWeaponDataSO ? CombatAction.Melee : CombatAction.Ranged;
    }

    #endregion

    #region Temporary Overrides (Shotgun mode, etc.)

    /// <summary>
    /// Temporarily overrides the RANGED weapon data for the player.
    /// This is intended for combos like Melee->Melee->Ranged => Shotgun.
    /// </summary>
    public void ApplyTemporaryRangedOverride(WeaponDataSO overrideWeaponData, float durationSeconds)
    {
        if (overrideWeaponData == null)
        {
            Debug.LogWarning("[WeaponSlots] Override data is null.", this);
            return;
        }

        if (durationSeconds <= 0f)
        {
            Debug.LogWarning("[WeaponSlots] durationSeconds must be > 0.", this);
            return;
        }

        // Stop previous override if any
        if (rangedOverrideRoutine != null)
            StopCoroutine(rangedOverrideRoutine);

        rangedOverrideRoutine = StartCoroutine(RangedOverrideRoutine(overrideWeaponData, durationSeconds));
    }

    private IEnumerator RangedOverrideRoutine(WeaponDataSO overrideWeaponData, float durationSeconds)
    {
        CacheWeaponDataIfNeeded();

        bool appliedAny = false;

        appliedAny |= TryApplyRangedOverrideToWeapon(mainWeapon, overrideWeaponData);
        appliedAny |= TryApplyRangedOverrideToWeapon(secondaryWeapon, overrideWeaponData);

        if (debugLogs)
            Debug.Log($"[WeaponSlots] Ranged override applied={appliedAny} for {durationSeconds:0.00}s", this);

        yield return new WaitForSeconds(durationSeconds);

        RestoreCachedWeaponData();

        if (debugLogs)
            Debug.Log("[WeaponSlots] Ranged override ended, restored original data.", this);

        rangedOverrideRoutine = null;
    }

    private bool TryApplyRangedOverrideToWeapon(WeaponBehaviour weapon, WeaponDataSO overrideWeaponData)
    {
        if (weapon == null) return false;

        // Only replace ranged weapons (keep melee as-is)
        bool isRanged = weapon.WeaponData is RangedWeaponDataSO || weapon.WeaponData is ExplosiveWeaponDataSO;
        if (!isRanged) return false;

        weapon.SetWeaponData(overrideWeaponData); // requires one method added in WeaponBehaviour
        weapon.SetAim(currentAim);

        return true;
    }

    private void CacheWeaponDataIfNeeded()
    {
        if (hasCachedData) return;

        cachedMainWeaponData = mainWeapon != null ? mainWeapon.WeaponData : null;
        cachedSecondaryWeaponData = secondaryWeapon != null ? secondaryWeapon.WeaponData : null;
        hasCachedData = true;
    }

    private void RestoreCachedWeaponData()
    {
        if (!hasCachedData) return;

        if (mainWeapon != null && cachedMainWeaponData != null)
        {
            mainWeapon.SetWeaponData(cachedMainWeaponData);
            mainWeapon.SetAim(currentAim);
        }

        if (secondaryWeapon != null && cachedSecondaryWeaponData != null)
        {
            secondaryWeapon.SetWeaponData(cachedSecondaryWeaponData);
            secondaryWeapon.SetAim(currentAim);
        }

        hasCachedData = false;
        cachedMainWeaponData = null;
        cachedSecondaryWeaponData = null;
    }




    #endregion
}