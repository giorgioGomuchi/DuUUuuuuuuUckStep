using UnityEngine;

public enum CombatAction
{
    Melee,
    Ranged,
    Dash,
    Special
}

[CreateAssetMenu(menuName = "Combat/Rhythm Combo Recipe", fileName = "SO_RhythmComboRecipe")]
public class RhythmComboRecipeSO : ScriptableObject
{
    [Header("Id")]
    [SerializeField] private string recipeId = "MeleeMeleeRanged_Shotgun";

    [Header("Sequence")]
    [SerializeField] private CombatAction[] sequence;

    [Header("Rules")]
    [SerializeField] private bool requireGoodOrPerfect = true;

    public string RecipeId => recipeId;
    public CombatAction[] Sequence => sequence;
    public bool RequireGoodOrPerfect => requireGoodOrPerfect;

    public int Length => sequence != null ? sequence.Length : 0;
}

/*
Unity setup:
- Create assets: Right click -> Create -> Combat -> Rhythm Combo Recipe
- Example: [Melee, Melee, Ranged] with id "MeleeMeleeRanged_Shotgun"
*/