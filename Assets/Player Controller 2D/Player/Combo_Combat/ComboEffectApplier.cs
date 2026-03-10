using System.Collections.Generic;
using UnityEngine;

public class ComboEffectApplier : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerComboDetector comboDetector;
    [SerializeField] private WeaponSlotsController weaponSlots;
    [SerializeField] private PlayerReferences playerReferences;

    [Header("Bindings")]
    [SerializeField] private List<ComboEffectBinding> bindings = new();

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;

    private void Awake()
    {
        if (comboDetector == null)
            comboDetector = GetComponentInChildren<PlayerComboDetector>();

        if (weaponSlots == null)
            weaponSlots = GetComponentInChildren<WeaponSlotsController>();

        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();
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

        ComboEffectSO effect = ResolveEffect(recipe);
        if (effect == null)
        {
            if (debugLogs)
                Debug.LogWarning($"[ComboEffectApplier] No effect bound for recipe: {recipe.RecipeId}", this);

            return;
        }

        ComboEffectContext context = new ComboEffectContext(
            recipe,
            weaponSlots,
            playerReferences);

        effect.Apply(context);

        if (debugLogs)
            Debug.Log($"[ComboEffectApplier] Applied effect for recipe: {recipe.RecipeId}", this);
    }

    private ComboEffectSO ResolveEffect(PlayerComboRecipeSO recipe)
    {
        if (bindings == null || bindings.Count == 0)
            return null;

        for (int i = 0; i < bindings.Count; i++)
        {
            ComboEffectBinding binding = bindings[i];
            if (binding == null)
                continue;

            if (binding.Recipe == recipe)
                return binding.Effect;
        }

        return null;
    }
}