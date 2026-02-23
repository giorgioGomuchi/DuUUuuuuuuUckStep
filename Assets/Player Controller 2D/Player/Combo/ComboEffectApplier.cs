using UnityEngine;

public class ComboEffectApplier : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RhythmCombatController rhythmCombat;
    [SerializeField] private WeaponSlotsController weaponSlots;

    [Header("Shotgun Combo")]
    [SerializeField] private string shotgunRecipeId = "MeleeMeleeRanged_Shotgun";
    [SerializeField] private WeaponDataSO shotgunWeaponData;
    [SerializeField] private float shotgunDurationSeconds = 6f;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void Awake()
    {
        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();
    }

    private void OnEnable()
    {
        if (rhythmCombat != null)
            rhythmCombat.onComboTriggered.AddListener(OnComboTriggered);
    }

    private void OnDisable()
    {
        if (rhythmCombat != null)
            rhythmCombat.onComboTriggered.RemoveListener(OnComboTriggered);
    }

    private void OnComboTriggered(RhythmComboRecipeSO recipe)
    {
        if (recipe == null) return;

        if (debugLogs)
            Debug.Log($"[ComboEffectApplier] Combo triggered: {recipe.RecipeId}", this);

        if (recipe.RecipeId != shotgunRecipeId)
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

        weaponSlots.ApplyTemporaryRangedOverride(shotgunWeaponData, shotgunDurationSeconds);
    }
}