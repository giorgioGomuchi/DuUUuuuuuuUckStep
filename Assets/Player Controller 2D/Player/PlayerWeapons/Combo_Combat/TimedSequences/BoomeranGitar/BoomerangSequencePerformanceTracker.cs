using System;
using UnityEngine;

[Serializable]
public class BoomerangSequencePerformanceTracker
{
    [SerializeField] private BoomerangSequencePerformance performance = new();

    public BoomerangSequencePerformance Performance => performance;

    public void ResetSequence()
    {
        performance.ResetAll();
    }

    public void BeginCycle(int cycleNumber)
    {
        performance.BeginCycle(cycleNumber);
    }

    public void CommitCurrentCycle()
    {
        performance.CommitCurrentCycle();
    }

    public void RegisterDamage(Collider2D target, BoomerangDamageActionType actionType)
    {
        performance.RegisterDamage(target, actionType);
    }
}