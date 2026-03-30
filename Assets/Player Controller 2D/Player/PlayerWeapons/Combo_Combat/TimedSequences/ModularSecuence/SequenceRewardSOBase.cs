using UnityEngine;

public abstract class SequenceRewardSOBase : ScriptableObject
{
    public abstract void Apply(
        SequenceRewardContextBase context,
        SequenceRewardResolution resolution,
        ISequenceActorAdapter actorAdapter);
}