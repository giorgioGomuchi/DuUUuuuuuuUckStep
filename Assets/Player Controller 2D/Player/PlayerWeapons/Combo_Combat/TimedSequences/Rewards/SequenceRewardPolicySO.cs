using UnityEngine;

public abstract class SequenceRewardPolicySO : ScriptableObject
{
    public abstract bool CanActivateReward(SequenceRewardContext context);
    public abstract float ResolveRewardDuration(SequenceRewardContext context, float baseDuration);
}