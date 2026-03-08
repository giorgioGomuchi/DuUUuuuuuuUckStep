using UnityEngine;

public abstract class ComboEffectSO : ScriptableObject
{
    [Header("Debug")]
    [SerializeField] protected bool debugLogs = false;

    public abstract void Apply(ComboEffectContext context);
}