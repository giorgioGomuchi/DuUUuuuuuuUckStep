using UnityEngine;

[CreateAssetMenu(fileName = "BoomerangLoopSequence", menuName = "Game/Player/Boomerang Loop Sequence")]
public class BoomerangLoopSequenceDefinitionSO : SequenceDefinitionSOBase
{
    [Header("Loop / Rhythm Gate")]
    [SerializeField] private bool requireRhythmOnInitialLaunch = true;

    [Header("Loop / Beat Timing")]
    [SerializeField] private bool useBeatBasedDecisionTiming = true;

    [Min(0.125f)]
    [SerializeField] private float returnHoldDurationBeats = 2f;

    [Min(0.125f)]
    [SerializeField] private float catchDecisionWindowBeats = 1f;

    [Min(0.03125f)]
    [SerializeField] private float decisionReleaseToReflectGraceBeats = 0.125f;

    [SerializeField] private bool useBeatSteppedRecall = true;

    [SerializeField, Min(1)] private int recallBeatOpportunities = 2;

    [SerializeField, Min(1)] private int postRedirectRecallBeatOpportunities = 2;

    [Min(0.125f)]
    [SerializeField] private float recallStepWindowBeats = 1f;

    [Min(0.125f)]
    [SerializeField] private float postRedirectRecallStepWindowBeats = 1f;

    [SerializeField] private bool waitForNextBeatOnRecall = true;
    [SerializeField] private bool waitForNextBeatOnDecision = true;

    [Header("Loop / Recall")]
    [SerializeField] private BoomerangLoopRecallSettings recall = new();

    [Header("Loop / Redirect Shot")]
    [SerializeField] private BoomerangLoopRedirectShotSettings redirectShot = new();

    [Header("Loop / Catch Decision")]
    [SerializeField] private BoomerangLoopCatchSettings catchDecision = new();

    [Header("Loop / Decision")]
    [SerializeField] private BoomerangLoopDecisionSettings decision = new();

    [Header("Loop / Recovery")]
    [SerializeField] private BoomerangLoopRecoverySettings recovery = new();

    [Header("Loop / Reward")]
    [SerializeField] private BoomerangLoopRewardSettings reward = new();

    [Header("Loop / Cleanup")]
    [SerializeField] private BoomerangLoopCleanupSettings cleanup = new();

    public bool RequireRhythmOnInitialLaunch => requireRhythmOnInitialLaunch;
    public bool UseBeatSteppedRecall => useBeatSteppedRecall;
    public int RecallBeatOpportunities => recallBeatOpportunities;
    public int PostRedirectRecallBeatOpportunities => postRedirectRecallBeatOpportunities;
    public float RecallStepWindowBeats => recallStepWindowBeats;
    public float PostRedirectRecallStepWindowBeats => postRedirectRecallStepWindowBeats;
    public TimedSequenceActionRule RecallRule => recall.recallRule;
    public float RecallWindowDuration => recall.recallWindowDuration;

    public float RecallShotRedirectAngleDegrees => redirectShot.redirectAngleDegrees;
    public float RecallShotRedirectWindowDuration => redirectShot.redirectWindowDuration;
    public float ShotRedirectOutboundDuration => redirectShot.redirectedOutboundDuration;
    public float PostRedirectRecallWindowDuration => redirectShot.postRedirectRecallWindowDuration;
    public float RecallShotRedirectDirectionBlend => redirectShot.redirectDirectionBlend;
    public float RecallShotRedirectBlendDuration => redirectShot.redirectBlendDuration;

    public float ReturnHoldDuration => catchDecision.returnHoldDuration;
    public TimedSequenceActionRule CatchDecisionRule => catchDecision.decisionRule;
    public float CatchDecisionWindowDuration => catchDecision.decisionWindowDuration;

    public float DecisionReleaseToReflectGraceSeconds => decision.releaseToReflectGraceSeconds;

    public float RecoveryCooldownOnEarlyRelease => recovery.recoveryCooldownOnEarlyRelease;
    public bool AllowDashDuringRecall => recovery.allowDashDuringRecall;
    public bool AllowDashDuringReflect => recovery.allowDashDuringReflect;
    public bool FailOnBadDash => recovery.failOnBadDash;
    public bool FailOnSwitchWeaponInput => recovery.failOnSwitchWeaponInput;
    public bool ClearWeaponOverrideOnFail => recovery.clearWeaponOverrideOnFail;
    public float FailCooldownDuration => recovery.failCooldownDuration;
    public bool KeepUIVisibleDuringFailCooldown => recovery.keepUIVisibleDuringFailCooldown;

    public float RecallShotRedirectDamageRadiusMultiplier => redirectShot.damageRadiusMultiplier;
    public float RecallShotRedirectAuraSpinSpeedDegPerSec => redirectShot.auraSpinSpeedDegPerSec;

    public bool RequireExplicitRewardTrigger => reward.requireExplicitRewardTrigger;
    public BoomerangLoopRewardTriggerInput RewardTriggerInput => reward.rewardTriggerInput;
    public bool RequireSuccessfulLoopCount => reward.requireSuccessfulLoopCount;
    public int MinSuccessfulLoopsForReward => reward.minSuccessfulLoopsForReward;
    public int MinReflectSuccessesForReward => reward.minReflectSuccessesForReward;
    public bool UseOrbitReward => reward.useOrbitReward;
    public float OrbitDuration => reward.orbitDuration;
    public float RequiredWeightedScore => reward.requiredWeightedScore;
    public float RelaunchScoreWeight => reward.relaunchScoreWeight;
    public float ReflectScoreWeight => reward.reflectScoreWeight;

    public bool DestroyProjectileOnFail => cleanup.destroyProjectileOnFail;
    public float DestroyProjectileOnFailDelay => cleanup.destroyProjectileOnFailDelay;

    public bool UseBeatBasedDecisionTiming => useBeatBasedDecisionTiming;
    public float ReturnHoldDurationBeats => returnHoldDurationBeats;
    public float CatchDecisionWindowBeats => catchDecisionWindowBeats;
    public float DecisionReleaseToReflectGraceBeats => decisionReleaseToReflectGraceBeats;

    public bool WaitForNextBeatOnRecall => waitForNextBeatOnRecall;
    public bool WaitForNextBeatOnDecision => waitForNextBeatOnDecision;

    public override bool IsValid()
    {
        return RequiredSteps > 0 &&
               RecallRule != null &&
               CatchDecisionRule != null &&
               RecallWindowDuration > 0f &&
               RecallShotRedirectWindowDuration > 0f &&
               ShotRedirectOutboundDuration > 0f &&
               PostRedirectRecallWindowDuration > 0f &&
               ReturnHoldDuration > 0f &&
               CatchDecisionWindowDuration > 0f &&
               DecisionReleaseToReflectGraceSeconds > 0f;
    }

    public float ResolveReturnHoldDuration(RhythmClock clock)
    {
        if (!useBeatBasedDecisionTiming || clock == null)
            return catchDecision.returnHoldDuration;

        return returnHoldDurationBeats * clock.SecondsPerBeat;
    }

    public float ResolveCatchDecisionWindowDuration(RhythmClock clock)
    {
        if (!useBeatBasedDecisionTiming || clock == null)
            return catchDecision.decisionWindowDuration;

        return catchDecisionWindowBeats * clock.SecondsPerBeat;
    }

    public float ResolveDecisionReleaseToReflectGraceSeconds(RhythmClock clock)
    {
        if (!useBeatBasedDecisionTiming || clock == null)
            return decision.releaseToReflectGraceSeconds;

        return decisionReleaseToReflectGraceBeats * clock.SecondsPerBeat;
    }

    public int ResolveRecallBeatOpportunities(bool isPostRedirect)
    {
        if (!useBeatSteppedRecall)
            return 1;

        return Mathf.Max(1, isPostRedirect ? postRedirectRecallBeatOpportunities : recallBeatOpportunities);
    }

    public float ResolveRecallStepWindowDuration(RhythmClock clock, bool isPostRedirect)
    {
        if (!useBeatSteppedRecall || clock == null)
            return isPostRedirect ? redirectShot.postRedirectRecallWindowDuration : recall.recallWindowDuration;

        float beats = isPostRedirect ? postRedirectRecallStepWindowBeats : recallStepWindowBeats;
        return beats * clock.SecondsPerBeat;
    }
}