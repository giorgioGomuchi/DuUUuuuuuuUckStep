using UnityEngine;

[CreateAssetMenu(
    fileName = "BoomerangOrbitRewardApply",
    menuName = "Game/Player/Timed Sequence Rewards/Boomerang Orbit Reward Apply")]
public class BoomerangOrbitRewardApplySO : SequenceRewardSOBase
{
    [Header("Defaults")]
    [SerializeField] private int defaultOrbitTurns = 0;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public override void Apply(
        SequenceRewardContextBase context,
        SequenceRewardResolution resolution,
        ISequenceActorAdapter actorAdapter)
    {
        // No se usa por el boomerang, porque su adapter aún no entra en ISequenceActorAdapter.
        Debug.LogWarning("[BoomerangOrbitRewardApplySO] Generic Apply not used in boomerang flow.");
    }

    public void ApplyToBoomerang(
        SequenceRewardContextBase context,
        SequenceRewardResolution resolution,
        BoomerangSequenceActorAdapter actorAdapter,
        float fallbackDuration,
        int fallbackTurns)
    {
        if (actorAdapter == null || !actorAdapter.IsValid)
        {
            Debug.LogError("[BoomerangOrbitRewardApplySO] Invalid boomerang actor adapter.");
            return;
        }

        if (!resolution.shouldApply)
            return;

        float finalDuration = resolution.duration > 0f ? resolution.duration : fallbackDuration;
        int finalTurns = fallbackTurns != 0 ? fallbackTurns : defaultOrbitTurns;

        actorAdapter.BeginReward(finalDuration, finalTurns);

        if (debugLogs)
        {
            Debug.Log(
                $"[BoomerangOrbitRewardApplySO] Applied orbit reward | Duration={finalDuration:F2}s | Turns={finalTurns}",
                actorAdapter);
        }
    }
}