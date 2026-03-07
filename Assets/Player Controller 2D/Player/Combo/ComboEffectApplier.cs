using UnityEngine;

public class ComboEffectApplier : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerComboDetector comboDetector;
    [SerializeField] private WeaponSlotsController weaponSlots;

    [Header("Shotgun Combo")]
    [SerializeField] private PlayerComboRecipeSO shotgunComboRecipe;
    [SerializeField] private WeaponDataSO shotgunWeaponData;
    [SerializeField] private WeaponSlotType shotgunOverrideSlot = WeaponSlotType.Main;
    [SerializeField] private int shotgunAmmo = 6;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void Awake()
    {
        if (comboDetector == null)
            comboDetector = GetComponentInChildren<PlayerComboDetector>();

        if (weaponSlots == null)
            weaponSlots = GetComponentInChildren<WeaponSlotsController>();
    }

    private void OnEnable()
    {
        if (comboDetector != null)
            comboDetector.OnComboTriggered += HandleComboTriggered;
    }

    private void OnDisable()
    {
        if (comboDetector != null)
            comboDetector.OnComboTriggered -= HandleComboTriggered;
    }

    private void HandleComboTriggered(PlayerComboRecipeSO recipe)
    {
        if (recipe == null)
            return;

        if (recipe != shotgunComboRecipe)
            return;

        if (weaponSlots == null)
        {
            Debug.LogError("[ComboEffectApplier] weaponSlots not assigned.", this);
            return;
        }

        if (shotgunWeaponData == null)
        {
            Debug.LogError("[ComboEffectApplier] shotgunWeaponData not assigned.", this);
            return;
        }

        weaponSlots.ApplyTemporaryWeaponOverride(
            shotgunOverrideSlot,
            shotgunWeaponData,
            shotgunAmmo);

        if (debugLogs)
        {
            Debug.Log(
                $"[ComboEffectApplier] Shotgun override applied | Slot={shotgunOverrideSlot} | Ammo={shotgunAmmo}",
                this);
        }
    }
}