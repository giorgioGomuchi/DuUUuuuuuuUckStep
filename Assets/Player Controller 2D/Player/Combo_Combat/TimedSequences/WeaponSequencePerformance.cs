using System.Collections.Generic;
using UnityEngine;

public class WeaponSequencePerformance
{
    private readonly HashSet<int> uniqueTargetIds = new();

    public int SuccessfulShots { get; private set; }
    public int PerfectShots { get; private set; }
    public int SuccessfulDashes { get; private set; }
    public int TotalHits { get; private set; }
    public int UniqueTargetsHitCount => uniqueTargetIds.Count;

    public void RegisterShot(TimingJudgement judgement)
    {
        SuccessfulShots++;

        if (judgement == TimingJudgement.Perfect)
            PerfectShots++;
    }

    public void RegisterDash(TimingJudgement judgement)
    {
        if (judgement == TimingJudgement.Good || judgement == TimingJudgement.Perfect)
            SuccessfulDashes++;
    }

    public void RegisterHit(Collider2D target)
    {
        if (target == null)
            return;

        TotalHits++;
        uniqueTargetIds.Add(target.GetInstanceID());
    }
}