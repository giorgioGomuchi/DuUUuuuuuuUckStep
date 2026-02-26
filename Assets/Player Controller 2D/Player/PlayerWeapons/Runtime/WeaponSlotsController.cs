using UnityEngine;

public class WeaponSlotsController : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private WeaponBehaviour mainWeapon;
    [SerializeField] private WeaponBehaviour secondaryWeapon;
    [SerializeField] private bool allowDualWield;

    [Header("Rhythm System (Optional Master Switch)")]
    [Tooltip("If false, ALL weapons fire in Normal mode regardless of WeaponDataSO settings.")]
    [SerializeField] private bool rhythmSystemEnabled = false;

    [SerializeField] private RhythmCombatController rhythmCombat;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private IWeaponState currentState;
    private SingleWieldState singleState;
    private DualWieldState dualState;

    private Vector2 currentAim;

    // Fire modes
    private IFireMode normalMode;
    private IFireMode rhythmMode;

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

        normalMode = new NormalFireMode();
        rhythmMode = new RhythmFireMode(rhythmCombat);

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

    /// <summary>
    /// Optional global switch (power-up / settings / accessibility).
    /// </summary>
    public void SetRhythmSystemEnabled(bool enabled)
    {
        rhythmSystemEnabled = enabled;

        if (debugLogs)
            Debug.Log($"[WeaponSlots] Rhythm system enabled = {rhythmSystemEnabled}", this);
    }

    #endregion

    #region Internal helpers used by states

    public void FireMain() => TryFire(mainWeapon);
    public void FireSecondaryWeapon() => TryFire(secondaryWeapon);

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

    #region Fire Routing (Per-weapon Rhythm Gate)

    private void TryFire(WeaponBehaviour weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return;

        CombatAction action = GetActionForWeapon(weapon);

        // Per-weapon routing + optional global override
        IFireMode mode = (rhythmSystemEnabled && weapon.WeaponData.useRhythmGate)
            ? rhythmMode
            : normalMode;

        mode.TryFire(weapon, action);

        if (debugLogs && rhythmSystemEnabled && weapon.WeaponData.useRhythmGate)
            Debug.Log($"[WeaponSlots] Fired with RHYTHM mode: {weapon.name}", this);
    }

    private CombatAction GetActionForWeapon(WeaponBehaviour weapon)
    {
        if (weapon == null) return CombatAction.Ranged;
        return weapon.WeaponData is MeleeAnimatedWeaponDataSO ? CombatAction.Melee : CombatAction.Ranged;
    }

    #endregion

    #region Temporary Overrides (Shotgun mode, etc.)

    public void CancelAllAttacks()
    {
        // Cancelación “limpia” base: desbloquea locks (boomerang) y permite que módulos cancelables limpien.
        mainWeapon?.CancelAttack();
        secondaryWeapon?.CancelAttack();
    }

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
        if (overrideActive) return;

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

        bool isRanged =
            weapon.WeaponData is RangedWeaponDataSO ||
            weapon.WeaponData is ExplosiveWeaponDataSO ||
            weapon.WeaponData is ShotgunWeaponDataSO;

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