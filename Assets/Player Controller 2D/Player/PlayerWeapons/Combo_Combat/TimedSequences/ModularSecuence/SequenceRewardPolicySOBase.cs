using UnityEngine;

public abstract class SequenceRewardPolicySOBase : ScriptableObject
{
    public abstract SequenceRewardResolution Evaluate(SequenceRewardContextBase context, SequenceDefinitionSOBase definition);

    public virtual SequenceRewardPreviewInfo BuildPreview(
        SequenceRewardContextBase context,
        SequenceDefinitionSOBase definition,
        SequenceRewardResolution resolution)
    {
        return new SequenceRewardPreviewInfo
        {
            stateText = resolution.shouldApply ? "READY" : "LOCKED",
            formulaText = string.Empty,
            resultText = resolution.ammo > 0
                ? $"Final: {resolution.ammo} ammo"
                : resolution.duration > 0f
                    ? $"Final: {resolution.duration:F2}s"
                    : string.Empty
        };
    }
}