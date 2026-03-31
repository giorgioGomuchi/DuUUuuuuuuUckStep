using UnityEngine;

[CreateAssetMenu(
    fileName = "BoomerangOrbitRewardApply",
    menuName = "Game/Player/Timed Sequence Rewards/Boomerang Orbit Reward Apply")]
public class BoomerangOrbitRewardApplySO : SequenceRewardSOBase
{
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public override void Apply(
        SequenceRewardContextBase context,
        SequenceRewardResolution resolution,
        ISequenceActorAdapter actorAdapter)
    {
        Debug.LogWarning("[BoomerangOrbitRewardApplySO] Generic Apply is not used by boomerang flow.");
    }

    public void ApplyToBoomerang(
        SequenceRewardContextBase context,
        SequenceRewardResolution resolution,
        BoomerangSequenceActorAdapter actorAdapter,
        float fallbackDuration)
    {
        if (actorAdapter == null || !actorAdapter.IsValid)
        {
            Debug.LogError("[BoomerangOrbitRewardApplySO] Invalid boomerang actor adapter.");
            return;
        }

        if (!resolution.shouldApply)
            return;

        float finalDuration = resolution.duration > 0f
            ? resolution.duration
            : fallbackDuration;

        actorAdapter.BeginReward(finalDuration, 0);

        if (debugLogs)
        {
            Debug.Log(
                $"[BoomerangOrbitRewardApplySO] Applied orbit reward | Duration={finalDuration:F2}s",
                actorAdapter);
        }
    }
}