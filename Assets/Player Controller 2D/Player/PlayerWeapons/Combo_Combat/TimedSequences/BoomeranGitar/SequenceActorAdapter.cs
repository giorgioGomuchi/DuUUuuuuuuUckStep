using UnityEngine;

public abstract class SequenceActorAdapter : MonoBehaviour
{
    public abstract bool IsValid { get; }

    public abstract void BeginReturn(float duration, float reflectActivationNormalized);
    public abstract void ResolveReflect(Vector2 direction);
    public abstract void BeginReward(float duration, int turns);
    public abstract void FailAndCleanup(float destroyDelay);

    public abstract bool CanReceiveReflect();
}