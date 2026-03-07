using UnityEngine;

public static class ComboDirectionUtility
{
    public static bool MatchesRelation(
        Vector2 actionDirection,
        Vector2 aimDirection,
        ActionAimRelationMode relationMode,
        float similarMinDot,
        float perpendicularMaxAbsDot)
    {
        if (relationMode == ActionAimRelationMode.Any)
            return true;

        if (actionDirection.sqrMagnitude <= 0.0001f || aimDirection.sqrMagnitude <= 0.0001f)
            return false;

        float dot = Vector2.Dot(actionDirection.normalized, aimDirection.normalized);

        switch (relationMode)
        {
            case ActionAimRelationMode.Similar:
                return dot >= similarMinDot;

            case ActionAimRelationMode.Opposite:
                return dot <= -similarMinDot;

            case ActionAimRelationMode.Perpendicular:
                return Mathf.Abs(dot) <= perpendicularMaxAbsDot;
        }

        return false;
    }
}