using UnityEngine;

[System.Serializable]
public class PlayerComboStepDefinition
{
    [Header("Action")]
    public PlayerActionType actionType = PlayerActionType.None;

    [Header("Direction vs Aim")]
    public ActionAimRelationMode actionVsAimRelation = ActionAimRelationMode.Any;

    [Range(0f, 1f)]
    public float similarMinDot = 0.75f;

    [Range(0f, 1f)]
    public float perpendicularMaxAbsDot = 0.25f;

    public bool Matches(PlayerActionData actionData)
    {
        if (actionData.ActionType != actionType)
            return false;

        return ComboDirectionUtility.MatchesRelation(
            actionData.ActionDirection,
            actionData.AimDirection,
            actionVsAimRelation,
            similarMinDot,
            perpendicularMaxAbsDot);
    }
}