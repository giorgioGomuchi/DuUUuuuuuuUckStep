using UnityEngine;

/// Owns weapon slot references, aim forwarding, swap/state facade, and resolved fire mode refresh.
/// Does not poll input directly and does not decide combo recipes or sequence rules.
public class WeaponSlotsController : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private WeaponBehaviour mainWeapon;
    [SerializeField] private WeaponBehaviour secondaryWeapon;
    [SerializeField] private bool allowDualWield = false;

    [Header("Refs")]
    [SerializeField] private WeaponOverrideController overrideController;
    [SerializeField] private WeaponFireExecutor fireExecutor;
    [SerializeField] private WeaponActionReporter actionReporter;
    [SerializeField] private WeaponSlotVisualController visualController;
    [SerializeField] private PlayerReferences playerReferences;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private IWeaponState currentState;
    private SingleWieldState singleState;
    private DualWieldState dualState;

    private Vector2 currentAim = Vector2.right;
    private bool rhythmSystemEnabled = true;

    public Vector2 CurrentAim => currentAim;
    public bool IsRhythmSystemEnabled => rhythmSystemEnabled;

    public WeaponBehaviour MainWeapon => mainWeapon;
    public WeaponBehaviour SecondaryWeapon => secondaryWeapon;

    private void Awake()
    {
        ResolveReferences();
        InitializeStates();
        SubscribeWeaponEvents();
        RefreshResolvedFireModes(forceRelease: false);
    }

    private void OnDestroy()
    {
        UnsubscribeWeaponEvents();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    private void ResolveReferences()
    {
        if (overrideController == null)
            overrideController = GetComponentInParent<WeaponOverrideController>();

        if (fireExecutor == null)
            fireExecutor = GetComponent<WeaponFireExecutor>();

        if (fireExecutor == null)
            fireExecutor = GetComponentInParent<WeaponFireExecutor>();

        if (actionReporter == null)
            actionReporter = GetComponent<WeaponActionReporter>();

        if (actionReporter == null)
            actionReporter = GetComponentInParent<WeaponActionReporter>();

        if (visualController == null)
            visualController = GetComponent<WeaponSlotVisualController>();

        if (visualController == null)
            visualController = GetComponentInParent<WeaponSlotVisualController>();

        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();
    }

    private void InitializeStates()
    {
        singleState = new SingleWieldState(this);
        dualState = new DualWieldState(this);

        currentState = allowDualWield ? dualState : singleState;
        currentState.Enter();
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
            string primaryWeaponName = mainWeapon != null && mainWeapon.WeaponData != null
                ? mainWeapon.WeaponData.weaponName
                : "NULL";

            string secondaryWeaponName = secondaryWeapon != null && secondaryWeapon.WeaponData != null
                ? secondaryWeapon.WeaponData.weaponName
                : "NULL";

            Debug.Log(
                $"[WeaponSlotsController] RefreshResolvedFireModes -> Primary={primaryWeaponName} ({primaryMode}) | Secondary={secondaryWeaponName} ({secondaryMode})",
                this);
        }
    }

    public bool FirePrimary()
    {
        if (currentState == null)
            return false;

        currentState.FirePrimary();
        return true;
    }

    public bool FireSecondary()
    {
        if (currentState == null)
            return false;

        currentState.FireSecondary();
        return true;
    }

    public bool FireSlot(WeaponSlotType slot)
    {
        if (fireExecutor == null)
            return false;

        bool didFire = fireExecutor.FireSlot(slot);
        if (!didFire)
            return false;

        actionReporter?.ReportSuccessfulFire(slot);
        return true;
    }

    public bool TryFireSlot(WeaponSlotType slot)
    {
        return FireSlot(slot);
    }

    public void SwitchWeapon()
    {
        currentState?.SwitchWeapon();
        ReportSwapAction();
    }

    private void ReportSwapAction()
    {
        actionReporter?.RecordSwitchWeaponAction(currentAim, currentAim);
    }

    public void PerformWeaponSwap()
    {
        WeaponBehaviour temp = mainWeapon;
        mainWeapon = secondaryWeapon;
        secondaryWeapon = temp;

        RefreshResolvedFireModes(forceRelease: true);
        SetAim(currentAim);

        if (debugLogs)
            Debug.Log("[WeaponSlotsController] PerformWeaponSwap executed.", this);
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
            Debug.Log($"[WeaponSlotsController] Rhythm system enabled = {rhythmSystemEnabled}", this);
    }

    public WeaponDataSO GetCurrentWeaponData(WeaponSlotType slot)
    {
        WeaponBehaviour weapon = GetWeaponBySlot(slot);
        return weapon != null ? weapon.WeaponData : null;
    }

    public WeaponBehaviour GetWeaponBySlot(WeaponSlotType slot)
    {
        return slot == WeaponSlotType.Main ? mainWeapon : secondaryWeapon;
    }

    public void SetSlotVisualVisible(WeaponSlotType slot, bool visible)
    {
        visualController?.SetSlotVisible(slot, visible);
    }

    public void SetAllSlotVisualsVisible(bool visible)
    {
        visualController?.SetAllVisible(visible);
    }

    public void ApplyTemporaryWeaponOverride(WeaponSlotType slot, WeaponDataSO overrideWeaponData, int ammoCount)
    {
        overrideController?.ApplyTemporaryWeaponOverride(slot, overrideWeaponData, ammoCount);
    }

    public void ApplyTemporaryWeaponOverrideForDuration(WeaponSlotType slot, WeaponDataSO overrideWeaponData, float durationSeconds)
    {
        overrideController?.ApplyTemporaryWeaponOverrideForDuration(slot, overrideWeaponData, durationSeconds);
    }

    public void CancelAllAttacks()
    {
        mainWeapon?.CancelAttack();
        secondaryWeapon?.CancelAttack();
    }

    public void SetSingleWield()
    {
        currentState?.Exit();
        currentState = singleState;
        currentState?.Enter();
    }

    public void SetDualWield()
    {
        currentState?.Exit();
        currentState = dualState;
        currentState?.Enter();
    }

   
}