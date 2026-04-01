using UnityEngine;

[CreateAssetMenu(
    fileName = "ShotgunConditionalRewardPolicy",
    menuName = "Game/Player/Timed Sequence Rewards/Shotgun Conditional Reward Policy")]
public class ShotgunConditionalRewardPolicySO : SequenceRewardPolicySOBase
{
    public enum RewardMode
    {
        Ammo = 0,
        Duration = 1
    }

    [Header("Mode")]
    [SerializeField] private RewardMode rewardMode = RewardMode.Ammo;

    [Header("Activation")]
    [Range(0f, 1f)]
    [SerializeField] private float requiredPelletHitRatio = 0.5f;

    [Min(0)]
    [SerializeField] private int minUniqueTargetsHit = 0;

    [SerializeField] private bool requireAtLeastOnePelletHit = true;

    [Header("Ammo")]
    [Min(1)]
    [SerializeField] private int baseAmmoCount = 2;

    [Min(1)]
    [SerializeField] private int maxAmmoCount = 8;

    [Header("Duration")]
    [Min(0.05f)]
    [SerializeField] private float baseDurationSeconds = 3f;

    [Min(0.05f)]
    [SerializeField] private float maxDurationSeconds = 8f;

    [Header("Scaling - Pellets / Unique")]
    [SerializeField] private bool scaleWithPerformance = true;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerExtraPelletHit = 0.10f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerUniqueTarget = 0.25f;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerEvery2ExtraPellets = 1;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerUniqueTarget = 0;

    [Header("Scaling - Timing Quality")]
    [Min(0)]
    [SerializeField] private int bonusAmmoPerPerfect = 1;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerEvery2Goods = 1;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerPerfect = 0.35f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerGood = 0.12f;

    public int ComputeRequiredPellets(int pelletsFiredTotal)
    {
        int safeTotal = Mathf.Max(0, pelletsFiredTotal);
        return Mathf.CeilToInt(safeTotal * requiredPelletHitRatio);
    }

    public override SequenceRewardResolution Evaluate(SequenceRewardContextBase context, SequenceDefinitionSOBase definition)
    {
        if (context == null || !context.sequenceCompleted)
            return SequenceRewardResolution.None;

        int pelletsFiredTotal = context.GetInt("pellets_fired_total");
        int pelletsHitTotal = context.GetInt("pellets_hit_total");
        int uniqueTargets = context.uniqueTargetCount;

        int perfectActivations = context.GetInt("perfect_activations");
        int goodActivations = context.GetInt("good_activations");

        if (requireAtLeastOnePelletHit && pelletsHitTotal <= 0)
            return SequenceRewardResolution.None;

        if (uniqueTargets < minUniqueTargetsHit)
            return SequenceRewardResolution.None;

        int pelletsRequired = ComputeRequiredPellets(pelletsFiredTotal);
        if (pelletsHitTotal < pelletsRequired)
            return SequenceRewardResolution.None;

        int extraPelletsAboveThreshold = Mathf.Max(0, pelletsHitTotal - pelletsRequired);

        if (rewardMode == RewardMode.Ammo)
        {
            int ammo = baseAmmoCount;

            if (scaleWithPerformance)
            {
                ammo += (extraPelletsAboveThreshold / 2) * bonusAmmoPerEvery2ExtraPellets;
                ammo += uniqueTargets * bonusAmmoPerUniqueTarget;
                ammo += perfectActivations * bonusAmmoPerPerfect;
                ammo += (goodActivations / 2) * bonusAmmoPerEvery2Goods;
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
            duration += extraPelletsAboveThreshold * bonusDurationPerExtraPelletHit;
            duration += uniqueTargets * bonusDurationPerUniqueTarget;
            duration += perfectActivations * bonusDurationPerPerfect;
            duration += goodActivations * bonusDurationPerGood;
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

        int pelletsFiredTotal = context.GetInt("pellets_fired_total");
        int pelletsHitTotal = context.GetInt("pellets_hit_total");
        int uniqueTargets = context.uniqueTargetCount;

        int perfectActivations = context.GetInt("perfect_activations");
        int goodActivations = context.GetInt("good_activations");

        int pelletsRequired = ComputeRequiredPellets(pelletsFiredTotal);

        string header =
            $"pellets {pelletsHitTotal}/{pelletsFiredTotal} | req {pelletsRequired} ({requiredPelletHitRatio:P0}) | unique {uniqueTargets} | perfect {perfectActivations} | good {goodActivations}";

        if (!resolution.shouldApply)
        {
            return new SequenceRewardPreviewInfo
            {
                stateText = "LOCKED",
                formulaText = header,
                resultText = "Final: blocked"
            };
        }

        int extraPelletsAboveThreshold = Mathf.Max(0, pelletsHitTotal - pelletsRequired);

        if (rewardMode == RewardMode.Ammo)
        {
            string formula =
                $"{baseAmmoCount}" +
                (scaleWithPerformance
                    ? $" + pellets(({extraPelletsAboveThreshold}/2)×{bonusAmmoPerEvery2ExtraPellets})" +
                      $" + unique({uniqueTargets}×{bonusAmmoPerUniqueTarget})" +
                      $" + perfect({perfectActivations}×{bonusAmmoPerPerfect})" +
                      $" + good(({goodActivations}/2)×{bonusAmmoPerEvery2Goods})"
                    : "");

            return new SequenceRewardPreviewInfo
            {
                stateText = "READY",
                formulaText = $"{header} | {formula}",
                resultText = $"Final: {resolution.ammo} ammo"
            };
        }

        string durationFormula =
            $"{baseDurationSeconds:F2}s" +
            (scaleWithPerformance
                ? $" + pellets({extraPelletsAboveThreshold}×{bonusDurationPerExtraPelletHit:F2})" +
                  $" + unique({uniqueTargets}×{bonusDurationPerUniqueTarget:F2})" +
                  $" + perfect({perfectActivations}×{bonusDurationPerPerfect:F2})" +
                  $" + good({goodActivations}×{bonusDurationPerGood:F2})"
                : "");

        return new SequenceRewardPreviewInfo
        {
            stateText = "READY",
            formulaText = $"{header} | {durationFormula}",
            resultText = $"Final: {resolution.duration:F2}s"
        };
    }
}