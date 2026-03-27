using UnityEngine;

[CreateAssetMenu(fileName = "BoomerangRewardPolicy", menuName = "Game/Player/Reward Policies/Boomerang Reward Policy")]
public class BoomerangRewardPolicySO : SequenceRewardPolicySO
{
    [Header("Activation")]
    [SerializeField] private bool requireDamageForReward = false;

    [Min(1)]
    [SerializeField] private int minUniqueEnemiesDamagedForReward = 1;

    [Header("Duration Scaling")]
    [SerializeField] private bool scaleDurationByUniqueEnemies = false;

    [Min(0f)]
    [SerializeField] private float extraDurationPerUniqueEnemy = 0.35f;

    [Min(0)]
    [SerializeField] private int maxEnemiesCountedForDuration = 10;

    [Header("Clamp Final Duration")]
    [SerializeField] private bool clampFinalDuration = true;

    [Min(0.05f)]
    [SerializeField] private float minFinalDuration = 0.5f;

    [Min(0.05f)]
    [SerializeField] private float maxFinalDuration = 6f;

    public override bool CanActivateReward(SequenceRewardContext context)
    {
        if (!requireDamageForReward)
            return true;

        return context != null &&
               context.TotalUniqueEnemiesDamaged >= Mathf.Max(0, minUniqueEnemiesDamagedForReward);
    }

    public override float ResolveRewardDuration(SequenceRewardContext context, float baseDuration)
    {
        float result = Mathf.Max(0.05f, baseDuration);

        if (scaleDurationByUniqueEnemies && context != null)
        {
            int countedEnemies = Mathf.Min(
                context.TotalUniqueEnemiesDamaged,
                Mathf.Max(0, maxEnemiesCountedForDuration));

            result += countedEnemies * Mathf.Max(0f, extraDurationPerUniqueEnemy);
        }

        if (clampFinalDuration)
        {
            float minDuration = Mathf.Max(0.05f, minFinalDuration);
            float maxDuration = Mathf.Max(minDuration, maxFinalDuration);
            result = Mathf.Clamp(result, minDuration, maxDuration);
        }

        return result;
    }
}