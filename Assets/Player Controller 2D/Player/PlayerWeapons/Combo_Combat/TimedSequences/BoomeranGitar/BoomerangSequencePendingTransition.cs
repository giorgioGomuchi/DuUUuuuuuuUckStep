using UnityEngine;

public enum BoomerangPendingTransitionType
{
    None = 0,
    BeginReturn = 1,
    ResolveReflect = 2
}

[System.Serializable]
public struct BoomerangSequencePendingTransition
{
    public BoomerangPendingTransitionType type;
    public float executeAtTime;
    public Vector2 reflectDirection;

    public bool IsActive => type != BoomerangPendingTransitionType.None;

    public void Clear()
    {
        type = BoomerangPendingTransitionType.None;
        executeAtTime = 0f;
        reflectDirection = Vector2.zero;
    }

    public void SetBeginReturn(float delay)
    {
        type = BoomerangPendingTransitionType.BeginReturn;
        executeAtTime = Time.time + Mathf.Max(0f, delay);
        reflectDirection = Vector2.zero;
    }

    public void SetResolveReflect(float delay, Vector2 direction)
    {
        type = BoomerangPendingTransitionType.ResolveReflect;
        executeAtTime = Time.time + Mathf.Max(0f, delay);
        reflectDirection = direction;
    }

    public bool IsReady()
    {
        return IsActive && Time.time >= executeAtTime;
    }
}