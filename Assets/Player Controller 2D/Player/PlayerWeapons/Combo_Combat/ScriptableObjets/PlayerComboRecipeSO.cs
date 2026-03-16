using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerComboRecipe", menuName = "Game/Player/Combo Recipe")]
public class PlayerComboRecipeSO : ScriptableObject
{
    [SerializeField] private string recipeId = "NewCombo";
    [SerializeField] private bool consumeMatchedActions = true;
    [SerializeField] private List<PlayerComboStepDefinition> steps = new();

    public string RecipeId => recipeId;
    public bool ConsumeMatchedActions => consumeMatchedActions;
    public int StepCount => steps != null ? steps.Count : 0;
    public IReadOnlyList<PlayerComboStepDefinition> Steps => steps;

    public bool MatchesTail(IReadOnlyList<PlayerActionData> recordedActions)
    {
        if (steps == null || steps.Count == 0)
            return false;

        if (recordedActions == null || recordedActions.Count < steps.Count)
            return false;

        int startIndex = recordedActions.Count - steps.Count;

        for (int i = 0; i < steps.Count; i++)
        {
            PlayerComboStepDefinition recipeStep = steps[i];
            PlayerActionData recordedAction = recordedActions[startIndex + i];

            if (!recipeStep.Matches(recordedAction))
                return false;
        }

        return true;
    }
}