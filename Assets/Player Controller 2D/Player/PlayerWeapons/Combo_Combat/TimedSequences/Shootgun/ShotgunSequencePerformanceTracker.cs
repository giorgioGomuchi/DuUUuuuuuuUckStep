using UnityEngine;

[System.Serializable]
public sealed class ShotgunSequencePerformanceTracker : SequencePerformanceTrackerBase
{
    [SerializeField] private int activationsSucceeded;
    [SerializeField] private int successfulDashes;

    [SerializeField] private int pelletsFiredTotal;
    [SerializeField] private int pelletsHitTotal;

    [SerializeField] private int sequenceShotsFired;
    [SerializeField] private int sequenceShotsThatLandedAtLeastOnePellet;
    [SerializeField] private int bestPelletHitCountInOneShot;

    [SerializeField] private int perfectActivations;
    [SerializeField] private int goodActivations;

    private int currentShotPelletHits;
    private bool shotOpen;

    public int ActivationsSucceeded => activationsSucceeded;
    public int SuccessfulDashes => successfulDashes;
    public int PelletsFiredTotal => pelletsFiredTotal;
    public int PelletsHitTotal => pelletsHitTotal;
    public int SequenceShotsFired => sequenceShotsFired;
    public int SequenceShotsThatLandedAtLeastOnePellet => sequenceShotsThatLandedAtLeastOnePellet;
    public int BestPelletHitCountInOneShot => bestPelletHitCountInOneShot;
    public int PerfectActivations => perfectActivations;
    public int GoodActivations => goodActivations;

    public override void ResetTracker()
    {
        base.ResetTracker();

        activationsSucceeded = 0;
        successfulDashes = 0;

        pelletsFiredTotal = 0;
        pelletsHitTotal = 0;

        sequenceShotsFired = 0;
        sequenceShotsThatLandedAtLeastOnePellet = 0;
        bestPelletHitCountInOneShot = 0;

        perfectActivations = 0;
        goodActivations = 0;

        currentShotPelletHits = 0;
        shotOpen = false;
    }

    public void RegisterActivation(TimingJudgement judgement)
    {
        activationsSucceeded++;

        switch (judgement)
        {
            case TimingJudgement.Perfect:
                perfectActivations++;
                RegisterPerfect();
                break;

            case TimingJudgement.Good:
                goodActivations++;
                RegisterGood();
                break;

            default:
                RegisterSuccess();
                break;
        }
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

    public void BeginSequenceShot(int pelletsFiredThisShot)
    {
        EndSequenceShotIfOpen();

        shotOpen = true;
        currentShotPelletHits = 0;

        sequenceShotsFired++;
        pelletsFiredTotal += Mathf.Max(0, pelletsFiredThisShot);
    }

    public void RegisterPelletHit(Collider2D target, float damage = 0f)
    {
        pelletsHitTotal++;
        currentShotPelletHits++;

        RegisterHit(target, damage);
    }

    public void EndSequenceShotIfOpen()
    {
        if (!shotOpen)
            return;

        if (currentShotPelletHits > 0)
            sequenceShotsThatLandedAtLeastOnePellet++;

        if (currentShotPelletHits > bestPelletHitCountInOneShot)
            bestPelletHitCountInOneShot = currentShotPelletHits;

        currentShotPelletHits = 0;
        shotOpen = false;
    }

    public override SequenceRewardContextBase BuildRewardContext(bool sequenceCompleted, int completedSteps, int attemptedSteps)
    {
        SequenceRewardContextBase context = BuildBaseContext(sequenceCompleted, completedSteps, attemptedSteps);

        context.SetInt("activations_succeeded", activationsSucceeded);
        context.SetInt("successful_dashes", successfulDashes);

        context.SetInt("pellets_fired_total", pelletsFiredTotal);
        context.SetInt("pellets_hit_total", pelletsHitTotal);

        context.SetInt("sequence_shots_fired", sequenceShotsFired);
        context.SetInt("sequence_shots_that_landed_pellets", sequenceShotsThatLandedAtLeastOnePellet);
        context.SetInt("best_pellets_hit_in_one_shot", bestPelletHitCountInOneShot);

        context.SetInt("perfect_activations", perfectActivations);
        context.SetInt("good_activations", goodActivations);

        return context;
    }

    public SequencePerformanceUISnapshot BuildUISnapshot(
        int currentProgress,
        int requiredProgress,
        bool rewardEligible,
        int pelletsRequiredForReward)
    {
        return new SequencePerformanceUISnapshot
        {
            currentProgress = currentProgress,
            requiredProgress = requiredProgress,

            metric1Label = "Pellets",
            metric1Value = $"{pelletsHitTotal}/{pelletsFiredTotal}",

            metric2Label = "Required",
            metric2Value = pelletsRequiredForReward.ToString(),

            metric3Label = "Unique",
            metric3Value = UniqueTargetCount.ToString(),

            metric4Label = "Best Shot",
            metric4Value = bestPelletHitCountInOneShot.ToString(),

            rewardLabel = "Reward",
            rewardEligible = rewardEligible
        };
    }
}