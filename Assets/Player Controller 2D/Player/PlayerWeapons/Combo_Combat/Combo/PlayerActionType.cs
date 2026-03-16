
using UnityEngine;

public enum PlayerActionType
{
    None = 0,
    Dash = 1,
    Melee = 2,
    Ranged = 3,
    SwitchWeapon = 4
}

public enum ActionAimRelationMode
{
    Any = 0,
    Similar = 1,
    Opposite = 2,
    Perpendicular = 3
}


[System.Serializable]
public struct PlayerActionData
{
    public PlayerActionType ActionType;
    public Vector2 ActionDirection;
    public Vector2 AimDirection;
    public string SourceId; //TODO: distinguir armas concretas o variantes de acción
    public int FrameIndex;

    public PlayerActionData(
        PlayerActionType actionType,
        Vector2 actionDirection,
        Vector2 aimDirection,
        string sourceId = "")
    {
        ActionType = actionType;
        ActionDirection = actionDirection.sqrMagnitude > 0.0001f ? actionDirection.normalized : Vector2.zero;
        AimDirection = aimDirection.sqrMagnitude > 0.0001f ? aimDirection.normalized : Vector2.zero;
        SourceId = sourceId;
        FrameIndex = Time.frameCount;
    }

    public float ActionVsAimDot
    {
        get
        {
            if (ActionDirection.sqrMagnitude <= 0.0001f || AimDirection.sqrMagnitude <= 0.0001f)
                return 0f;

            return Vector2.Dot(ActionDirection.normalized, AimDirection.normalized);
        }
    }
}