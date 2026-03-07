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

    [Header("Combo")]
    [SerializeField] private PlayerActionRecorder actionRecorder;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private IWeaponState currentState;
    private SingleWieldState singleState;
    private DualWieldState dualState;

    private Vector2 currentAim = Vector2.right;

    private IFireMode normalMode;
    private IFireMode rhythmMode;

    // Override por munición
    private bool overrideActive;
    private WeaponSlotType overrideSlot;
    private WeaponDataSO cachedOverrideOriginalData;
    private WeaponDataSO activeOverrideData;
    private int overrideAmmoRemaining;


    public System.Action<WeaponSlotType> OnWeaponOverrideEnded;


    private void Awake()
    {
        singleState = new SingleWieldState(this);
        dualState = new DualWieldState(this);

        currentState = allowDualWield ? dualState : singleState;

        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();

        if (actionRecorder == null)
            actionRecorder = GetComponentInParent<PlayerActionRecorder>();

        normalMode = new NormalFireMode();
        rhythmMode = new RhythmFireMode(rhythmCombat);

        currentState.Enter();
    }

    #region Public API (called from PlayerRoot)

    public void FirePrimary() => currentState.FirePrimary();
    public void FireSecondary() => currentState.FireSecondary();
    public void SwitchWeapon() => currentState.SwitchWeapon();

    public void SetAim(Vector2 direction)
    {
        currentAim = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        mainWeapon?.SetAim(currentAim);
        secondaryWeapon?.SetAim(currentAim);
    }

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
        RecordSwitchWeaponAction();
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

        IFireMode mode = (rhythmSystemEnabled && weapon.WeaponData.useRhythmGate)
            ? rhythmMode
            : normalMode;

        bool didFire = mode.TryFire(weapon, action);

        if (!didFire)
            return;

        RecordWeaponAction(weapon);

        if (overrideActive)
            ConsumeOverrideAmmo(weapon);

        if (debugLogs && rhythmSystemEnabled && weapon.WeaponData.useRhythmGate)
            Debug.Log($"[WeaponSlots] Fired with RHYTHM mode: {weapon.name}", this);
    }

    private CombatAction GetActionForWeapon(WeaponBehaviour weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return CombatAction.Ranged;

        return weapon.WeaponData is MeleeAnimatedWeaponDataSO
            ? CombatAction.Melee
            : CombatAction.Ranged;
    }

    #endregion

    #region Combo Recording

    private void RecordWeaponAction(WeaponBehaviour weapon)
    {
        if (weapon == null || actionRecorder == null || weapon.WeaponData == null)
            return;

        PlayerActionType actionType = WeaponActionTypeResolver.Resolve(weapon.WeaponData);

        if (actionType == PlayerActionType.None)
            return;

        PlayerActionData actionData = new PlayerActionData(
            actionType,
            currentAim,
            currentAim,
            weapon.WeaponData.weaponName
        );

        actionRecorder.RecordAction(actionData);

        if (debugLogs)
            Debug.Log($"[WeaponSlots] Combo action recorded: {actionType} ({weapon.WeaponData.weaponName})", this);
    }

    private void RecordSwitchWeaponAction()
    {
        if (actionRecorder == null)
            return;

        PlayerActionData actionData = new PlayerActionData(
            PlayerActionType.SwitchWeapon,
            currentAim,
            currentAim,
            "SwitchWeapon"
        );

        actionRecorder.RecordAction(actionData);

        if (debugLogs)
            Debug.Log("[WeaponSlots] Combo action recorded: SwitchWeapon", this);
    }

    #endregion

    #region Temporary Override By Ammo

    public void CancelAllAttacks()
    {
        mainWeapon?.CancelAttack();
        secondaryWeapon?.CancelAttack();
    }

    public void ApplyTemporaryWeaponOverride(WeaponSlotType slot, WeaponDataSO overrideWeaponData, int ammoCount)
    {
        if (overrideWeaponData == null)
        {
            Debug.LogWarning("[WeaponSlots] Override data is null.", this);
            return;
        }

        if (ammoCount <= 0)
        {
            Debug.LogWarning("[WeaponSlots] ammoCount must be > 0.", this);
            return;
        }

        WeaponBehaviour targetWeapon = GetWeaponBySlot(slot);
        if (targetWeapon == null || targetWeapon.WeaponData == null)
        {
            Debug.LogWarning($"[WeaponSlots] Target slot {slot} has no valid weapon.", this);
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
                $"[WeaponSlots] Override type mismatch. Slot={slot}, Current={currentData.GetType().Name}, Override={overrideWeaponData.GetType().Name}",
                this);
            return;
        }

        if (!overrideActive)
        {
            cachedOverrideOriginalData = currentData;
        }
        else if (overrideSlot != slot)
        {
            Debug.LogWarning("[WeaponSlots] Another override is already active on a different slot.", this);
            return;
        }

        overrideActive = true;
        overrideSlot = slot;
        activeOverrideData = overrideWeaponData;
        overrideAmmoRemaining = ammoCount;

        targetWeapon.SetWeaponData(overrideWeaponData);
        targetWeapon.SetAim(currentAim);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponSlots] Override ON | Slot={slot} | Weapon={overrideWeaponData.weaponName} | Ammo={overrideAmmoRemaining}",
                this);
        }
    }

    private WeaponBehaviour GetWeaponBySlot(WeaponSlotType slot)
    {
        return slot == WeaponSlotType.Main ? mainWeapon : secondaryWeapon;
    }

    

    private void ConsumeOverrideAmmo(WeaponBehaviour firedWeapon)
    {
        if (!overrideActive)
            return;

        WeaponBehaviour overrideWeapon = GetWeaponBySlot(overrideSlot);
        if (firedWeapon != overrideWeapon)
            return;

        overrideAmmoRemaining--;
        overrideAmmoRemaining = Mathf.Max(0, overrideAmmoRemaining);

        if (debugLogs)
            Debug.Log($"[WeaponSlots] Override ammo left: {overrideAmmoRemaining}", this);

        if (overrideAmmoRemaining == 0)
            EndWeaponOverride();
    }

    private void EndWeaponOverride()
    {
        if (!overrideActive)
            return;

        WeaponBehaviour targetWeapon = GetWeaponBySlot(overrideSlot);

        if (targetWeapon != null && cachedOverrideOriginalData != null)
        {
            targetWeapon.SetWeaponData(cachedOverrideOriginalData);
            targetWeapon.SetAim(currentAim);
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponSlots] Override OFF | Slot={overrideSlot} | Restored={(cachedOverrideOriginalData != null ? cachedOverrideOriginalData.weaponName : "null")}",
                this);
        }

        OnWeaponOverrideEnded?.Invoke(overrideSlot);

        overrideActive = false;
        cachedOverrideOriginalData = null;
        activeOverrideData = null;
        overrideAmmoRemaining = 0;
    }

    #endregion
}