using UnityEngine;

public class WeaponSlotsController : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private WeaponBehaviour mainWeapon;
    [SerializeField] private WeaponBehaviour secondaryWeapon;
    [SerializeField] private bool allowDualWield;

    [Header("Rhythm System")]
    [Tooltip("If false, ALL weapons fire in Normal mode regardless of WeaponDataSO settings.")]
    [SerializeField] private bool rhythmSystemEnabled = false;
    [SerializeField] private RhythmCombatController rhythmCombat;

    [Header("Combo")]
    [SerializeField] private PlayerActionRecorder actionRecorder;

    [Header("Override")]
    [SerializeField] private WeaponOverrideController overrideController;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private IWeaponState currentState;
    private SingleWieldState singleState;
    private DualWieldState dualState;

    private IFireMode normalMode;
    private IFireMode rhythmMode;

    private Vector2 currentAim = Vector2.right;

    public WeaponBehaviour MainWeapon => mainWeapon;
    public WeaponBehaviour SecondaryWeapon => secondaryWeapon;
    public Vector2 CurrentAim => currentAim;
    public WeaponOverrideController OverrideController => overrideController;

    private void Awake()
    {
        InitializeStates();
        InitializeFireModes();
        ResolveExtraRefs();
    }

    private void InitializeStates()
    {
        singleState = new SingleWieldState(this);
        dualState = new DualWieldState(this);

        currentState = allowDualWield ? dualState : singleState;
        currentState.Enter();
    }

    private void InitializeFireModes()
    {
        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();

        normalMode = new NormalFireMode();
        rhythmMode = new RhythmFireMode(rhythmCombat);
    }

    private void ResolveExtraRefs()
    {
        if (actionRecorder == null)
            actionRecorder = GetComponentInParent<PlayerActionRecorder>();

        if (overrideController == null)
            overrideController = GetComponent<WeaponOverrideController>();

        if (overrideController == null)
            overrideController = GetComponentInParent<WeaponOverrideController>();
    }

    #region Public API

    public void FirePrimary()
    {
        currentState.FirePrimary();
    }

    public void FireSecondary()
    {
        currentState.FireSecondary();
    }

    public void SwitchWeapon()
    {
        currentState.SwitchWeapon();
    }

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

    public WeaponDataSO GetCurrentWeaponData(WeaponSlotType slot)
    {
        WeaponBehaviour weapon = GetWeaponBySlot(slot);
        return weapon != null ? weapon.WeaponData : null;
    }

    public void ApplyTemporaryWeaponOverride(WeaponSlotType slot, WeaponDataSO overrideWeaponData, int ammoCount)
    {
        overrideController?.ApplyTemporaryWeaponOverride(slot, overrideWeaponData, ammoCount);
    }

    public void ClearActiveOverride()
    {
        overrideController?.ClearActiveOverride();
    }

    public void CancelAllAttacks()
    {
        mainWeapon?.CancelAttack();
        secondaryWeapon?.CancelAttack();
    }

    public WeaponBehaviour GetWeaponBySlot(WeaponSlotType slot)
    {
        return slot == WeaponSlotType.Main ? mainWeapon : secondaryWeapon;
    }

    #endregion

    #region State Helpers

    public void FireMain()
    {
        TryFireWeapon(mainWeapon);
    }

    public void FireSecondaryWeapon()
    {
        TryFireWeapon(secondaryWeapon);
    }

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

    #region Fire

    private void TryFireWeapon(WeaponBehaviour weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return;

        CombatAction action = ResolveCombatAction(weapon);
        IFireMode fireMode = ResolveFireMode(weapon);

        bool didFire = fireMode.TryFire(weapon, action);
        if (!didFire)
            return;

        RecordWeaponAction(weapon);
        overrideController?.ConsumeAmmoIfNeeded(weapon);

        if (debugLogs && fireMode == rhythmMode)
            Debug.Log($"[WeaponSlots] Fired with RHYTHM mode: {weapon.name}", this);
    }

    private CombatAction ResolveCombatAction(WeaponBehaviour weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return CombatAction.Ranged;

        return weapon.WeaponData is MeleeAnimatedWeaponDataSO
            ? CombatAction.Melee
            : CombatAction.Ranged;
    }

    private IFireMode ResolveFireMode(WeaponBehaviour weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return normalMode;

        bool useRhythm = rhythmSystemEnabled && weapon.WeaponData.useRhythmGate;
        return useRhythm ? rhythmMode : normalMode;
    }

    #endregion

    #region Action Recording

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
}