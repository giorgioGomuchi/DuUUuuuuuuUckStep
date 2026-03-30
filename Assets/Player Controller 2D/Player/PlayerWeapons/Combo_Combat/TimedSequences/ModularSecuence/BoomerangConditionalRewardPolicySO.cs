using UnityEngine;

[CreateAssetMenu(
    fileName = "BoomerangConditionalRewardPolicy",
    menuName = "Game/Player/Timed Sequence Rewards/Boomerang Conditional Reward Policy")]
public class BoomerangConditionalRewardPolicySO : SequenceRewardPolicySOBase
{
    [Header("Activation")]
    [SerializeField] private bool requireDamageForReward = false;

    [Min(0)]
    [SerializeField] private int minUniqueEnemiesDamagedForReward = 1;

    [Min(0)]
    [SerializeField] private int minCompletedCyclesForReward = 0;

    [Min(0)]
    [SerializeField] private int minCyclesWithHitsForReward = 0;

    [Header("Duration Scaling")]
    [SerializeField] private bool scaleDurationByUniqueEnemies = false;

    [Min(0f)]
    [SerializeField] private float extraDurationPerUniqueEnemy = 0.35f;

    [SerializeField] private bool scaleDurationByCyclesWithHits = false;

    [Min(0f)]
    [SerializeField] private float extraDurationPerCycleWithHits = 0.25f;

    [Min(0)]
    [SerializeField] private int maxEnemiesCountedForDuration = 10;

    [Header("Clamp Final Duration")]
    [SerializeField] private bool clampFinalDuration = true;

    [Min(0.05f)]
    [SerializeField] private float minFinalDuration = 0.5f;

    [Min(0.05f)]
    [SerializeField] private float maxFinalDuration = 6f;

    [Header("Base")]
    [Min(0.05f)]
    [SerializeField] private float baseDurationSeconds = 2f;

    public override SequenceRewardResolution Evaluate(SequenceRewardContextBase context, SequenceDefinitionSOBase definition)
    {
        if (context == null || !context.sequenceCompleted)
            return SequenceRewardResolution.None;

        int uniqueEnemies = context.GetInt("boomerang_total_unique_enemies");
        int completedCycles = context.GetInt("boomerang_completed_cycles");
        int cyclesWithHits = context.GetInt("boomerang_cycles_with_hits");

        if (requireDamageForReward && uniqueEnemies < Mathf.Max(0, minUniqueEnemiesDamagedForReward))
            return SequenceRewardResolution.None;

        if (completedCycles < Mathf.Max(0, minCompletedCyclesForReward))
            return SequenceRewardResolution.None;

        if (cyclesWithHits < Mathf.Max(0, minCyclesWithHitsForReward))
            return SequenceRewardResolution.None;

        float duration = Mathf.Max(0.05f, baseDurationSeconds);

        if (scaleDurationByUniqueEnemies)
        {
            int countedEnemies = Mathf.Min(uniqueEnemies, Mathf.Max(0, maxEnemiesCountedForDuration));
            duration += countedEnemies * Mathf.Max(0f, extraDurationPerUniqueEnemy);
        }

        if (scaleDurationByCyclesWithHits)
        {
            duration += Mathf.Max(0, cyclesWithHits) * Mathf.Max(0f, extraDurationPerCycleWithHits);
        }

        if (clampFinalDuration)
        {
            float minDuration = Mathf.Max(0.05f, minFinalDuration);
            float maxDuration = Mathf.Max(minDuration, maxFinalDuration);
            duration = Mathf.Clamp(duration, minDuration, maxDuration);
        }

        return new SequenceRewardResolution
        {
            shouldApply = duration > 0f,
            duration = duration,
            ammo = 0,
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

        int uniqueEnemies = context.GetInt("boomerang_total_unique_enemies");
        int completedCycles = context.GetInt("boomerang_completed_cycles");
        int cyclesWithHits = context.GetInt("boomerang_cycles_with_hits");

        if (!resolution.shouldApply)
        {
            return new SequenceRewardPreviewInfo
            {
                stateText = "LOCKED",
                formulaText =
                    $"unique {uniqueEnemies}/{Mathf.Max(0, minUniqueEnemiesDamagedForReward)} | " +
                    $"cycles {completedCycles}/{Mathf.Max(0, minCompletedCyclesForReward)} | " +
                    $"hitCycles {cyclesWithHits}/{Mathf.Max(0, minCyclesWithHitsForReward)}",
                resultText = "Final: blocked"
            };
        }

        string formula = $"{baseDurationSeconds:F2}s";

        if (scaleDurationByUniqueEnemies)
        {
            int countedEnemies = Mathf.Min(uniqueEnemies, Mathf.Max(0, maxEnemiesCountedForDuration));
            formula += $" + ({countedEnemies}×{extraDurationPerUniqueEnemy:F2})";
        }

        if (scaleDurationByCyclesWithHits)
        {
            formula += $" + ({cyclesWithHits}×{extraDurationPerCycleWithHits:F2})";
        }

        return new SequenceRewardPreviewInfo
        {
            stateText = "READY",
            formulaText = formula,
            resultText = $"Final: {resolution.duration:F2}s"
        };
    }
}