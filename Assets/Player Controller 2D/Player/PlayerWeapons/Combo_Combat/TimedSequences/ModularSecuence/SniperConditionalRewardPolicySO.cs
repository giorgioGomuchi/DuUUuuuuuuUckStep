using UnityEngine;

[CreateAssetMenu(
    fileName = "SniperConditionalRewardPolicy",
    menuName = "Game/Player/Timed Sequence Rewards/Sniper Conditional Reward Policy")]
public class SniperConditionalRewardPolicySO : SequenceRewardPolicySOBase
{
    public enum RewardMode
    {
        Ammo = 0,
        Duration = 1
    }

    [Header("Mode")]
    [SerializeField] private RewardMode rewardMode = RewardMode.Duration;

    [Header("Activation")]
    [SerializeField] private bool requireAtLeastOneHit = true;

    [Min(0)]
    [SerializeField] private int minShotsThatHit = 1;

    [Min(0)]
    [SerializeField] private int minUniqueTargetsHit = 0;

    [Min(0)]
    [SerializeField] private int minPerfectShots = 0;

    [Min(0)]
    [SerializeField] private int minPerfectShotsThatHit = 0;

    [SerializeField] private bool requireDifferentTargets = false;

    [Header("Ammo")]
    [Min(1)]
    [SerializeField] private int baseAmmoCount = 3;

    [Min(1)]
    [SerializeField] private int maxAmmoCount = 12;

    [Header("Duration")]
    [Min(0.05f)]
    [SerializeField] private float baseDurationSeconds = 3f;

    [Min(0.05f)]
    [SerializeField] private float maxDurationSeconds = 6f;

    [Header("Scaling")]
    [SerializeField] private bool scaleWithPerformance = true;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerPerfectShot = 0.15f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerShotThatHit = 0.15f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerUniqueTarget = 0.25f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerPerfectShotThatHit = 0.25f;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerPerfectShot = 0;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerUniqueTarget = 0;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerPerfectShotThatHit = 0;

    public override SequenceRewardResolution Evaluate(SequenceRewardContextBase context, SequenceDefinitionSOBase definition)
    {
        if (context == null || !context.sequenceCompleted)
            return SequenceRewardResolution.None;

        int shotsThatHit = context.GetInt("shots_that_hit");
        int perfectShots = context.GetInt("perfect_shots");
        int perfectShotsThatHit = context.GetInt("perfect_shots_that_hit");
        int uniqueTargets = context.uniqueTargetCount;

        if (requireAtLeastOneHit && shotsThatHit <= 0)
            return SequenceRewardResolution.None;

        if (shotsThatHit < minShotsThatHit)
            return SequenceRewardResolution.None;

        if (uniqueTargets < minUniqueTargetsHit)
            return SequenceRewardResolution.None;

        if (perfectShots < minPerfectShots)
            return SequenceRewardResolution.None;

        if (perfectShotsThatHit < minPerfectShotsThatHit)
            return SequenceRewardResolution.None;

        if (requireDifferentTargets && uniqueTargets < 2)
            return SequenceRewardResolution.None;

        if (rewardMode == RewardMode.Ammo)
        {
            int ammo = baseAmmoCount;

            if (scaleWithPerformance)
            {
                ammo += perfectShots * bonusAmmoPerPerfectShot;
                ammo += uniqueTargets * bonusAmmoPerUniqueTarget;
                ammo += perfectShotsThatHit * bonusAmmoPerPerfectShotThatHit;
            }

            ammo = Mathf.Clamp(ammo, 1, maxAmmoCount);

            return new SequenceRewardResolution
            {
                shouldApply = ammo > 0,
                ammo = ammo,
                duration = 0f,
                magnitude = 0f
            };
        }

        float duration = baseDurationSeconds;

        if (scaleWithPerformance)
        {
            duration += perfectShots * bonusDurationPerPerfectShot;
            duration += shotsThatHit * bonusDurationPerShotThatHit;
            duration += uniqueTargets * bonusDurationPerUniqueTarget;
            duration += perfectShotsThatHit * bonusDurationPerPerfectShotThatHit;
        }

        duration = Mathf.Clamp(duration, 0.05f, maxDurationSeconds);

        return new SequenceRewardResolution
        {
            shouldApply = duration > 0f,
            ammo = 0,
            duration = duration,
            magnitude = 0f
        };
    }

    public override SequenceRewardPreviewInfo BuildPreview(
    SequenceRewardContextBase context,
    SequenceDefinitionSOBase definition,
    SequenceRewardResolution resolution)
    {
        if (context == null)
            return SequenceRewardPreviewInfo.Empty;

        int shotsThatHit = context.GetInt("shots_that_hit");
        int perfectShots = context.GetInt("perfect_shots");
        int perfectShotsThatHit = context.GetInt("perfect_shots_that_hit");
        int uniqueTargets = context.uniqueTargetCount;

        if (!resolution.shouldApply)
        {
            return new SequenceRewardPreviewInfo
            {
                stateText = "LOCKED",
                formulaText =
                    $"hits {shotsThatHit}/{Mathf.Max(0, minShotsThatHit)} | " +
                    $"unique {uniqueTargets}/{Mathf.Max(0, minUniqueTargetsHit)} | " +
                    $"perfect {perfectShots}/{Mathf.Max(0, minPerfectShots)} | " +
                    $"perfectHit {perfectShotsThatHit}/{Mathf.Max(0, minPerfectShotsThatHit)}",
                resultText = "Final: blocked"
            };
        }

        if (rewardMode == RewardMode.Ammo)
        {
            string formula =
                $"{baseAmmoCount}" +
                (scaleWithPerformance ? $" + ({perfectShots}×{bonusAmmoPerPerfectShot}) + ({uniqueTargets}×{bonusAmmoPerUniqueTarget}) + ({perfectShotsThatHit}×{bonusAmmoPerPerfectShotThatHit})" : "");

            return new SequenceRewardPreviewInfo
            {
                stateText = "READY",
                formulaText = formula,
                resultText = $"Final: {resolution.ammo} ammo"
            };
        }

        string durationFormula =
            $"{baseDurationSeconds:F2}s" +
            (scaleWithPerformance
                ? $" + ({perfectShots}×{bonusDurationPerPerfectShot:F2}) + ({shotsThatHit}×{bonusDurationPerShotThatHit:F2}) + ({uniqueTargets}×{bonusDurationPerUniqueTarget:F2}) + ({perfectShotsThatHit}×{bonusDurationPerPerfectShotThatHit:F2})"
                : "");

        return new SequenceRewardPreviewInfo
        {
            stateText = "READY",
            formulaText = durationFormula,
            resultText = $"Final: {resolution.duration:F2}s"
        };
    }
}