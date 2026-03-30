using UnityEngine;

[System.Serializable]
public sealed class WeaponSequencePerformanceTracker : SequencePerformanceTrackerBase
{
    [SerializeField] private int shotsFired;
    [SerializeField] private int shotsThatHit;
    [SerializeField] private int successfulDashes;

    [SerializeField] private int perfectShots;
    [SerializeField] private int goodShots;

    [SerializeField] private int perfectShotsThatHit;
    [SerializeField] private int goodShotsThatHit;

    [SerializeField] private int currentShotHitCount;
    [SerializeField] private int maxTargetsHitInOneShot;

    private bool shotOpen;
    private TimingJudgement currentShotJudgement = TimingJudgement.Fail;
    public bool HasOpenShot => shotOpen;

    public int ShotsFired => shotsFired;
    public int ShotsThatHit => shotsThatHit;
    public int SuccessfulDashes => successfulDashes;
    public int PerfectShots => perfectShots;
    public int GoodShots => goodShots;
    public int PerfectShotsThatHit => perfectShotsThatHit;
    public int GoodShotsThatHit => goodShotsThatHit;
    public int MaxTargetsHitInOneShot => maxTargetsHitInOneShot;

    public override void ResetTracker()
    {
        base.ResetTracker();

        shotsFired = 0;
        shotsThatHit = 0;
        successfulDashes = 0;
        perfectShots = 0;
        goodShots = 0;
        perfectShotsThatHit = 0;
        goodShotsThatHit = 0;

        currentShotHitCount = 0;
        maxTargetsHitInOneShot = 0;
        shotOpen = false;
        currentShotJudgement = TimingJudgement.Fail;
    }

    public void BeginShot(TimingJudgement judgement)
    {
        Debug.Log($"[WeaponSequencePerformanceTracker] BeginShot | judgement={judgement} | shotsFired={shotsFired}");
        shotsFired++;
        shotOpen = true;
        currentShotJudgement = judgement;
        currentShotHitCount = 0;

        switch (judgement)
        {
            case TimingJudgement.Perfect:
                perfectShots++;
                RegisterPerfect();
                break;

            case TimingJudgement.Good:
                goodShots++;
                RegisterGood();
                break;

            default:
                RegisterSuccess();
                break;
        }
    }

    public void EndShot()
    {
        Debug.Log($"[WeaponSequencePerformanceTracker] EndShot | hitCountInShot={currentShotHitCount} | shotsThatHit={shotsThatHit} | perfectShotsThatHit={perfectShotsThatHit} | uniqueTargets={UniqueTargetCount}");
        if (!shotOpen)
            return;

        if (currentShotHitCount > 0)
        {
            shotsThatHit++;

            if (currentShotJudgement == TimingJudgement.Perfect)
                perfectShotsThatHit++;

            if (currentShotJudgement == TimingJudgement.Good)
                goodShotsThatHit++;
        }

        if (currentShotHitCount > maxTargetsHitInOneShot)
            maxTargetsHitInOneShot = currentShotHitCount;

        shotOpen = false;
        currentShotJudgement = TimingJudgement.Fail;
        currentShotHitCount = 0;
    }

    public void RegisterShotHit(Collider2D target, float damage = 0f)
    {
        Debug.Log($"[WeaponSequencePerformanceTracker] RegisterShotHit | currentShotHitCount={currentShotHitCount + 1} | target={target.name}");
        currentShotHitCount++;
        RegisterHit(target, damage);
    }

    public void RegisterDash(TimingJudgement judgement)
    {
        successfulDashes++;

        switch (judgement)
        {
            case TimingJudgement.Perfect:
                RegisterPerfect();
                break;

            case TimingJudgement.Good:
                RegisterGood();
                break;

            default:
                RegisterSuccess();
                break;
        }
    }

    public override SequenceRewardContextBase BuildRewardContext(bool sequenceCompleted, int completedSteps, int attemptedSteps)
    {
        SequenceRewardContextBase context = BuildBaseContext(sequenceCompleted, completedSteps, attemptedSteps);

        context.SetInt("shots_fired", shotsFired);
        context.SetInt("shots_that_hit", shotsThatHit);
        context.SetInt("successful_dashes", successfulDashes);
        context.SetInt("perfect_shots", perfectShots);
        context.SetInt("good_shots", goodShots);
        context.SetInt("perfect_shots_that_hit", perfectShotsThatHit);
        context.SetInt("good_shots_that_hit", goodShotsThatHit);
        context.SetInt("max_targets_hit_in_one_shot", maxTargetsHitInOneShot);

        return context;
    }

    public void EndShotIfOpen()
    {
        if (shotOpen)
            EndShot();
    }

    public SequencePerformanceUISnapshot BuildUISnapshot(int currentProgress, int requiredProgress, bool rewardEligible)
    {
        return new SequencePerformanceUISnapshot
        {
            currentProgress = currentProgress,
            requiredProgress = requiredProgress,

            metric1Label = "Hits",
            metric1Value = shotsThatHit.ToString(),

            metric2Label = "Unique",
            metric2Value = UniqueTargetCount.ToString(),

            metric3Label = "Perfect",
            metric3Value = perfectShots.ToString(),

            metric4Label = "Perfect Hit",
            metric4Value = perfectShotsThatHit.ToString(),

            rewardLabel = "Reward",
            rewardEligible = rewardEligible
        };
    }
}