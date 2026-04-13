using UnityEngine;

[CreateAssetMenu(fileName = "BoomerangLoopSequence", menuName = "Game/Player/Boomerang Loop Sequence")]
public class BoomerangLoopSequenceDefinitionSO : BoomerangSequenceDefinitionSO
{


    [Header("Loop / Recall")]
    [SerializeField] private BoomerangLoopRecallSettings recall = new();

    [Header("Loop / Catch Decision")]
    [SerializeField] private BoomerangLoopCatchSettings catchDecision = new();

    [Header("Loop / Reflect")]
    [SerializeField] private BoomerangLoopReflectSettings reflect = new();

    [Header("Loop / Recovery")]
    [SerializeField] private BoomerangLoopRecoverySettings recovery = new();

    [Header("Loop / Reward")]
    [SerializeField] private BoomerangLoopRewardSettings reward = new();

    [Header("Loop / Cleanup")]
    [SerializeField] private BoomerangLoopCleanupSettings cleanup = new();



    public bool AllowRelaunchBranch => true;
    public bool AllowReflectBranch => true;

    public float RecoveryCooldownOnEarlyRelease => recovery.recoveryCooldownOnEarlyRelease;

    public float RequiredWeightedScore => reward.requiredWeightedScore;
    public float RelaunchScoreWeight => reward.relaunchScoreWeight;
    public float ReflectScoreWeight => reward.reflectScoreWeight;

    public new TimedSequenceActionRule RecallRule => recall.recallRule;
    public new float RecallWindowDuration => recall.recallWindowDuration;

    //public new TimedSequenceActionRule ReflectRule => reflect.reflectRule;
    //public new float ReflectWindowDuration => reflect.reflectWindowDuration;
    //public new float ReflectActivationNormalized => reflect.reflectActivationNormalized;

    public TimedSequenceActionRule CatchDecisionRule => catchDecision.decisionRule;
    public float CatchDecisionWindowDuration => catchDecision.decisionWindowDuration;
    public float ReturnHoldDuration => catchDecision.returnHoldDuration;

    public bool RequireExplicitRewardTrigger => reward.requireExplicitRewardTrigger;
    public BoomerangLoopRewardTriggerInput RewardTriggerInput => reward.rewardTriggerInput;
    public bool RequireSuccessfulLoopCount => reward.requireSuccessfulLoopCount;
    public int MinSuccessfulLoopsForReward => reward.minSuccessfulLoopsForReward;
    public int MinReflectSuccessesForReward => reward.minReflectSuccessesForReward;

    public float FailCooldownDuration => recovery.failCooldownDuration;

    public bool KeepUIVisibleDuringFailCooldown => recovery.keepUIVisibleDuringFailCooldown;


    public override bool IsValid()
    {
        return RecallWindowDuration > 0f &&
               ReturnHoldDuration > 0f &&
               CatchDecisionWindowDuration > 0f &&
               RecallRule != null &&
               CatchDecisionRule != null;
    }
}