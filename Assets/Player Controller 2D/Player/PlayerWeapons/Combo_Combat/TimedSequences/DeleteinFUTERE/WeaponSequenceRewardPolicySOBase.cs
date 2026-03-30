using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponSequenceRewardPolicy",
    menuName = "Game/Player/Timed Sequence Rewards/Weapon Sequence Reward Policy")]
public class WeaponSequenceRewardPolicySOBase : SequenceRewardPolicySOBase
{
    public enum RewardMode
    {
        Ammo = 0,
        Duration = 1
    }

    [Header("Mode")]
    [SerializeField] private RewardMode rewardMode = RewardMode.Duration;

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
    [SerializeField] private float bonusDurationPerPerfect = 0.15f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerHit = 0.08f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerUniqueTarget = 0.2f;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerPerfect = 0;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerUniqueTarget = 0;

    public override SequenceRewardResolution Evaluate(SequenceRewardContextBase context, SequenceDefinitionSOBase definition)
    {
        if (context == null || !context.sequenceCompleted)
            return SequenceRewardResolution.None;

        if (rewardMode == RewardMode.Ammo)
        {
            int ammo = baseAmmoCount;

            if (scaleWithPerformance)
            {
                ammo += context.perfectCount * bonusAmmoPerPerfect;
                ammo += context.uniqueTargetCount * bonusAmmoPerUniqueTarget;
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
            duration += context.perfectCount * bonusDurationPerPerfect;
            duration += context.hitCount * bonusDurationPerHit;
            duration += context.uniqueTargetCount * bonusDurationPerUniqueTarget;
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
}