using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public abstract class SequencePerformanceTrackerBase
{
    [SerializeField] protected int successfulActions;
    [SerializeField] protected int perfectCount;
    [SerializeField] protected int goodCount;
    [SerializeField] protected int hitCount;
    [SerializeField] protected float totalDamage;

    protected readonly HashSet<int> uniqueTargetIds = new();

    public int SuccessfulActions => successfulActions;
    public int PerfectCount => perfectCount;
    public int GoodCount => goodCount;
    public int HitCount => hitCount;
    public float TotalDamage => totalDamage;
    public int UniqueTargetCount => uniqueTargetIds.Count;

    public virtual void ResetTracker()
    {
        successfulActions = 0;
        perfectCount = 0;
        goodCount = 0;
        hitCount = 0;
        totalDamage = 0f;
        uniqueTargetIds.Clear();
    }

    public virtual void RegisterSuccess()
    {
        successfulActions++;
    }

    public virtual void RegisterPerfect()
    {
        perfectCount++;
        successfulActions++;
    }

    public virtual void RegisterGood()
    {
        goodCount++;
        successfulActions++;
    }

    public virtual void RegisterHit(Collider2D target, float damage = 0f)
    {
        hitCount++;
        totalDamage += Mathf.Max(0f, damage);

        if (target != null)
            uniqueTargetIds.Add(target.GetInstanceID());
    }

    public virtual void RegisterHit(int targetInstanceId, float damage = 0f)
    {
        hitCount++;
        totalDamage += Mathf.Max(0f, damage);

        if (targetInstanceId != 0)
            uniqueTargetIds.Add(targetInstanceId);
    }

    public virtual SequenceRewardContextBase BuildBaseContext(bool sequenceCompleted, int completedSteps, int attemptedSteps)
    {
        return new SequenceRewardContextBase
        {
            sequenceCompleted = sequenceCompleted,
            completedSteps = completedSteps,
            attemptedSteps = attemptedSteps,
            successfulActions = successfulActions,
            perfectCount = perfectCount,
            goodCount = goodCount,
            hitCount = hitCount,
            uniqueTargetCount = uniqueTargetIds.Count,
            totalDamage = totalDamage
        };
    }

    public abstract SequenceRewardContextBase BuildRewardContext(bool sequenceCompleted, int completedSteps, int attemptedSteps);
}