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
    [SerializeField] private bool cancelAttackOnFail = false;

    [Header("Temporary Overrides")]
    [SerializeField] private bool debugLogs = true;

    private IWeaponState currentState;
    private SingleWieldState singleState;
    private DualWieldState dualState;

    private Vector2 currentAim;

    // Override (no coroutine)
    private WeaponDataSO cachedMainWeaponData;
    private WeaponDataSO cachedSecondaryWeaponData;
    private bool overrideActive;
    private float overrideEndTime;

    private void Awake()
    {
        singleState = new SingleWieldState(this);
        dualState = new DualWieldState(this);

        currentState = allowDualWield ? dualState : singleState;

        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();

        currentState.Enter();
    }

    private void Update()
    {
        if (overrideActive && Time.time >= overrideEndTime)
            EndRangedOverride();
    }

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

        RhythmInputResult result = rhythmCombat != null
            ? rhythmCombat.RegisterAttack(action)
            : new RhythmInputResult { action = action, quality = RhythmHitQuality.Good, distanceToBeat = 0f, beatPhase01 = 0f, wasEarly = true };

        if (cancelAttackOnFail && result.quality == RhythmHitQuality.Fail)
        {
            if (debugLogs) Debug.Log($"[WeaponSlots] Cancel attack (Fail). action={action}", this);
            return;
        }

        if (result.quality == RhythmHitQuality.Perfect)
        {
            weapon.SetNextAttackDamageMultiplier(perfectDamageMultiplier);

            if (debugLogs) Debug.Log($"[WeaponSlots] Perfect => dmg x{perfectDamageMultiplier:0.00} ({weapon.name})", this);
        }

        weapon.TryFire();
    }

    private CombatAction GetActionForWeapon(WeaponBehaviour weapon)
    {
        if (weapon == null) return CombatAction.Ranged;
        return weapon.WeaponData is MeleeAnimatedWeaponDataSO ? CombatAction.Melee : CombatAction.Ranged;
    }

    #endregion

    #region Temporary Overrides (Shotgun mode, etc.)

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

        CacheWeaponDataIfNeeded();
        ApplyOverrideToRangedWeapons(overrideWeaponData);

        overrideActive = true;
        overrideEndTime = Time.time + durationSeconds;

        if (debugLogs)
            Debug.Log($"[WeaponSlots] Ranged override ON for {durationSeconds:0.00}s", this);
    }

    private void CacheWeaponDataIfNeeded()
    {
        if (overrideActive) return; // already cached

        cachedMainWeaponData = mainWeapon != null ? mainWeapon.WeaponData : null;
        cachedSecondaryWeaponData = secondaryWeapon != null ? secondaryWeapon.WeaponData : null;
    }

    private void ApplyOverrideToRangedWeapons(WeaponDataSO overrideWeaponData)
    {
        TryApplyRangedOverrideToWeapon(mainWeapon, overrideWeaponData);
        TryApplyRangedOverrideToWeapon(secondaryWeapon, overrideWeaponData);
    }

    private bool TryApplyRangedOverrideToWeapon(WeaponBehaviour weapon, WeaponDataSO overrideWeaponData)
    {
        if (weapon == null) return false;

        bool isRanged = weapon.WeaponData is RangedWeaponDataSO || weapon.WeaponData is ExplosiveWeaponDataSO || weapon.WeaponData is ShotgunWeaponDataSO;
        if (!isRanged) return false;

        weapon.SetWeaponData(overrideWeaponData);
        weapon.SetAim(currentAim);
        return true;
    }

    private void EndRangedOverride()
    {
        overrideActive = false;

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

        cachedMainWeaponData = null;
        cachedSecondaryWeaponData = null;

        if (debugLogs)
            Debug.Log("[WeaponSlots] Ranged override OFF (restored).", this);
    }

    #endregion
}