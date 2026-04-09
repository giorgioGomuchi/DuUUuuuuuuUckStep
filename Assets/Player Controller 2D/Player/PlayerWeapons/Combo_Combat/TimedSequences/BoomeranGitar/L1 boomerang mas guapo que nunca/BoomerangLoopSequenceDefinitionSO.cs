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

    public new TimedSequenceActionRule ReflectRule => reflect.reflectRule;
    public new float ReflectWindowDuration => reflect.reflectWindowDuration;
    public new float ReflectActivationNormalized => reflect.reflectActivationNormalized;


    public TimedSequenceActionRule ReleaseRule => catchDecision.releaseRule;
    public float ReturnHoldDuration => catchDecision.returnHoldDuration;
    public float CatchReleaseWindowDuration => catchDecision.releaseWindowDuration;
    public float ReflectDelayAfterCatch => catchDecision.reflectDelayAfterCatch;




    public override bool IsValid()
    {
        return RecallWindowDuration > 0f &&
               ReturnHoldDuration > 0f &&
               CatchReleaseWindowDuration > 0f &&
               ReflectWindowDuration > 0f &&
               RecallRule != null &&
               ReleaseRule != null &&
               ReflectRule != null;
    }
}