using UnityEngine;

public abstract class SequenceDefinitionSOBase : ScriptableObject
{
    [Header("Identification")]
    [SerializeField] private string sequenceId = "Sequence";

    [Header("Progress")]
    [Min(1)]
    [SerializeField] private int requiredSteps = 1;

    [Header("UI")]
    [SerializeField] private Vector3 playerUIWorldOffset = new(0f, 1.4f, 0f);

    [Header("Reward")]
    [SerializeField] private SequenceRewardPolicySOBase rewardPolicy;
    [SerializeField] private SequenceRewardSOBase completionReward;

    public string SequenceId => sequenceId;
    public int RequiredSteps => requiredSteps;
    public Vector3 PlayerUIWorldOffset => playerUIWorldOffset;
    public SequenceRewardPolicySOBase RewardPolicy => rewardPolicy;
    public SequenceRewardSOBase CompletionReward => completionReward;

    public virtual bool SupportsGenericRuntime => false;

    public virtual float StartupDelay => 0f;
    public virtual float DecisionWindowDuration => 0.5f;
    public virtual float CompletionDelay => 0f;

    public virtual bool FailOnTimeout => false;
    public virtual bool FailOnWrongAction => false;
    public virtual bool FailOnForbiddenInput => false;

    public abstract bool IsValid();
}