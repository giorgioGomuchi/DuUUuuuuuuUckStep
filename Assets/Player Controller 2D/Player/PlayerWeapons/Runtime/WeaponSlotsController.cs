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

    [Header("Player")]
    [SerializeField] private PlayerReferences playerReferences;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private IWeaponState currentState;
    private SingleWieldState singleState;
    private DualWieldState dualState;

    private IFireMode normalMode;
    private IFireMode rhythmMode;

    private Vector2 currentAim = Vector2.right;

    public WeaponBehaviour MainWeapon => mainWeapon;
    public Vector2 CurrentAim => currentAim;
    public WeaponOverrideController OverrideController => overrideController;

    private void Awake()
    {
        ResolveRefs();
        InitializeStates();
        InitializeFireModes();
        SubscribeWeaponEvents();
        RefreshResolvedFireModes(forceRelease: false);
    }

    private void OnDestroy()
    {
        UnsubscribeWeaponEvents();
    }

    private void ResolveRefs()
    {
        if (actionRecorder == null)
            actionRecorder = GetComponentInParent<PlayerActionRecorder>();

        if (overrideController == null)
            overrideController = GetComponent<WeaponOverrideController>();

        if (overrideController == null)
            overrideController = GetComponentInParent<WeaponOverrideController>();

        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();

        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();
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
        normalMode = new NormalFireMode();
        rhythmMode = new RhythmFireMode(rhythmCombat);
    }

    private void SubscribeWeaponEvents()
    {
        if (mainWeapon != null)
            mainWeapon.OnWeaponDataChanged += HandleWeaponDataChanged;

        if (secondaryWeapon != null)
            secondaryWeapon.OnWeaponDataChanged += HandleWeaponDataChanged;
    }

    private void UnsubscribeWeaponEvents()
    {
        if (mainWeapon != null)
            mainWeapon.OnWeaponDataChanged -= HandleWeaponDataChanged;

        if (secondaryWeapon != null)
            secondaryWeapon.OnWeaponDataChanged -= HandleWeaponDataChanged;
    }

    private void HandleWeaponDataChanged(WeaponBehaviour _, WeaponDataSO __)
    {
        RefreshResolvedFireModes(forceRelease: false);
    }

    public void RefreshResolvedFireModes(bool forceRelease)
    {
        if (playerReferences == null || playerReferences.Input == null)
            return;

        FireInputMode primaryMode = WeaponFireModeResolver.Resolve(mainWeapon != null ? mainWeapon.WeaponData : null);
        FireInputMode secondaryMode = WeaponFireModeResolver.Resolve(secondaryWeapon != null ? secondaryWeapon.WeaponData : null);

        playerReferences.Input.ApplyWeaponResolvedFireModes(primaryMode, secondaryMode, forceRelease);

        if (debugLogs)
        {
            string primaryWeapon = mainWeapon != null && mainWeapon.WeaponData != null
                ? mainWeapon.WeaponData.weaponName
                : "NULL";

            string secondaryWeapon_ = secondaryWeapon != null && secondaryWeapon.WeaponData != null
                ? secondaryWeapon.WeaponData.weaponName
                : "NULL";

            Debug.Log(
                $"[WeaponSlots] RefreshResolvedFireModes -> PrimaryWeapon={primaryWeapon} PrimaryMode={primaryMode} | SecondaryWeapon={secondaryWeapon} SecondaryMode={secondaryMode}",
                this);
        }
    }

    public bool FirePrimary()
    {
        return FireMain();
    }

    public bool FireSecondary()
    {
        return FireSecondaryWeapon();
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

    public void ApplyTemporaryWeaponOverrideForDuration(WeaponSlotType slot, WeaponDataSO overrideWeaponData, float duration)
    {
        overrideController?.ApplyTemporaryWeaponOverrideForDuration(slot, overrideWeaponData, duration);
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

    public bool FireMain()
    {
        return TryFireWeapon(mainWeapon);
    }

    public bool FireSecondaryWeapon()
    {
        return TryFireWeapon(secondaryWeapon);
    }

    public void SwapWeapons()
    {
        UnsubscribeWeaponEvents();

        WeaponBehaviour temp = mainWeapon;
        mainWeapon = secondaryWeapon;
        secondaryWeapon = temp;

        SubscribeWeaponEvents();

        SetAim(currentAim);
        RecordSwitchWeaponAction();
        RefreshResolvedFireModes(forceRelease: false);
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

    private bool TryFireWeapon(WeaponBehaviour weapon)
    {
        if (weapon == null || weapon.WeaponData == null)
            return false;

        CombatAction action = ResolveCombatAction(weapon);
        IFireMode fireMode = ResolveFireMode(weapon);

        bool didFire = fireMode.TryFire(weapon, action);
        if (!didFire)
            return false;

        RecordWeaponAction(weapon);
        overrideController?.ConsumeAmmoIfNeeded(weapon);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponSlots] Fire success -> weapon={weapon.WeaponName} action={action} fireMode={fireMode.GetType().Name}",
                this);
        }

        return true;
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
}