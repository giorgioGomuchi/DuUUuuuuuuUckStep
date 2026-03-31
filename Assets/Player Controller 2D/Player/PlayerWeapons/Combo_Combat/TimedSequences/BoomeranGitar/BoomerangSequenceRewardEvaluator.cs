using UnityEngine;

public sealed class BoomerangSequenceRewardEvaluator
{
    public SequenceRewardContextBase BuildContext(
        BoomerangSequencePerformance performance,
        int completedCycles,
        int attemptedCycles)
    {
        if (performance == null)
            return null;

        return performance.BuildRewardContextBase(
            sequenceCompleted: true,
            completedSteps: completedCycles,
            attemptedSteps: attemptedCycles);
    }

    public SequenceRewardResolution Evaluate(
        SequenceRewardPolicySOBase rewardPolicy,
        SequenceRewardContextBase context)
    {
        if (rewardPolicy == null || context == null)
            return SequenceRewardResolution.None;

        return rewardPolicy.Evaluate(context, null);
    }

    public SequenceRewardPreviewInfo BuildPreview(
        SequenceRewardPolicySOBase rewardPolicy,
        SequenceRewardContextBase context,
        SequenceRewardResolution resolution)
    {
        if (rewardPolicy == null || context == null)
            return SequenceRewardPreviewInfo.Empty;

        return rewardPolicy.BuildPreview(context, null, resolution);
    }

    public bool IsRewardEligible(
        SequenceRewardPolicySOBase rewardPolicy,
        BoomerangSequencePerformance performance,
        int completedCycles,
        int attemptedCycles)
    {
        SequenceRewardContextBase context = BuildContext(performance, completedCycles, attemptedCycles);
        SequenceRewardResolution resolution = Evaluate(rewardPolicy, context);
        return resolution.shouldApply;
    }
}