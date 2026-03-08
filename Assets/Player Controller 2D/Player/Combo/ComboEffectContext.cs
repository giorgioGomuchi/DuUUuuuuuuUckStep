using UnityEngine;

public class ComboEffectContext
{
    public PlayerComboRecipeSO TriggeredRecipe { get; }
    public WeaponSlotsController WeaponSlots { get; }
    public PlayerReferences PlayerReferences { get; }

    public ComboEffectContext(
        PlayerComboRecipeSO triggeredRecipe,
        WeaponSlotsController weaponSlots,
        PlayerReferences playerReferences)
    {
        TriggeredRecipe = triggeredRecipe;
        WeaponSlots = weaponSlots;
        PlayerReferences = playerReferences;
    }
}