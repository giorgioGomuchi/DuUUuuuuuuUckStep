using System;
using UnityEngine;

public class ShotgunSequenceActorAdapter : MonoBehaviour, ISequenceActorAdapter
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;

    [Header("Input During Sequence")]
    [SerializeField] private bool forcePrimarySinglePress = true;
    [SerializeField] private bool forceSecondarySinglePress = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private ShotgunSequenceDefinitionSO activeDefinition;

    public bool IsValid => playerReferences != null;
    public WeaponOverrideController OverrideController => playerReferences != null ? playerReferences.WeaponOverride : null;

    public event Action<SequenceFailReason> OnExternalSequenceFail;

    private void Awake()
    {
        ResolveReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    private void ResolveReferences()
    {
        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();
    }

    public void Bind() { }
    public void Unbind() { }

    public void OnSequenceStarted(SequenceDefinitionSOBase definition)
    {
        activeDefinition = definition as ShotgunSequenceDefinitionSO;

        if (activeDefinition == null)
        {
            OnExternalSequenceFail?.Invoke(SequenceFailReason.InvalidDefinition);
            return;
        }

        playerReferences?.Combat?.CancelAllAttacks();

        playerReferences?.Input?.BeginSequenceInputOverride(
            forcePrimarySinglePress,
            forceSecondarySinglePress);

        if (activeDefinition.SequenceWeaponData == null)
        {
            Debug.LogError("[ShotgunSequenceActorAdapter] SequenceWeaponData missing.", this);
            OnExternalSequenceFail?.Invoke(SequenceFailReason.InvalidDefinition);
            return;
        }

        playerReferences?.WeaponOverride?.ApplyTemporaryWeaponOverride(
            activeDefinition.TargetSlot,
            activeDefinition.SequenceWeaponData,
            9999);

        if (debugLogs)
            Debug.Log($"[ShotgunSequenceActorAdapter] Started -> {activeDefinition.SequenceId}", this);
    }

    public void OnSequenceCancelled()
    {
        CleanupSequenceState();
    }

    public void OnSequenceFailed(SequenceFailReason reason)
    {
        CleanupSequenceState();

        if (debugLogs)
            Debug.LogWarning($"[ShotgunSequenceActorAdapter] Failed -> {reason}", this);
    }

    public void OnSequenceCompleted()
    {
        playerReferences?.Combat?.CancelAllAttacks();

        // Muy importante: quitar la shotgun base temporal de la secuencia.
        //playerReferences?.WeaponOverride?.ClearActiveOverride();

        playerReferences?.Input?.EndSequenceInputOverride();
        playerReferences?.WeaponSlots?.RefreshResolvedFireModes(forceRelease: false);

        if (debugLogs && activeDefinition != null)
            Debug.Log($"[ShotgunSequenceActorAdapter] Completed -> {activeDefinition.SequenceId}", this);

        activeDefinition = null;
    }

    public void EnterStepWindow(int stepIndex) { }
    public void ExitStepWindow(int stepIndex) { }
    public void TickSequence(float deltaTime) { }

// Valida el input rítmico de activación de secuencia.
    public SequenceActionResult TryHandlePrimaryAction(float normalizedWindowTime)
    {
        if (activeDefinition == null)
            return SequenceActionResult.Rejected;

        TimingJudgement judgement = EvaluateTiming(normalizedWindowTime, activeDefinition.ShootRule);

        if (judgement == TimingJudgement.Fail)
            return SequenceActionResult.Rejected;

        playerReferences?.Input?.ClearBufferedInputs();

        return new SequenceActionResult
        {
            accepted = true,
            completedStep = false,
            completedSequence = false,
            perfect = judgement == TimingJudgement.Perfect,
            good = judgement == TimingJudgement.Good,
            hits = 0,
            damage = 0f
        };
    }

    public SequenceActionResult TryHandleSecondaryAction(float normalizedWindowTime)
    {
        return SequenceActionResult.Rejected;
    }

    public SequenceActionResult TryHandleDashAction(float normalizedWindowTime)
    {
        if (activeDefinition == null)
            return SequenceActionResult.Rejected;

        TimingJudgement judgement = EvaluateTiming(normalizedWindowTime, activeDefinition.DashRule);

        if (judgement == TimingJudgement.Fail)
            return SequenceActionResult.Rejected;

        Vector2 dashDirection = ResolveSequenceDashDirection();

        bool didDash =
            playerReferences != null &&
            playerReferences.StateMachine != null &&
            playerReferences.StateMachine.TryDash(
                dashDirection,
                ignoreCooldown: true,
                recordAction: false);

        if (!didDash)
            return SequenceActionResult.Rejected;

        playerReferences?.Input?.ClearBufferedInputs();

        return new SequenceActionResult
        {
            accepted = true,
            completedStep = false,
            completedSequence = false,
            perfect = judgement == TimingJudgement.Perfect,
            good = judgement == TimingJudgement.Good,
            hits = 0,
            damage = 0f
        };
    }


    public void ApplyReward(SequenceRewardSOBase reward, SequenceRewardResolution resolution, SequenceRewardContextBase context)
    {
        reward?.Apply(context, resolution, this);
    }

    public void RegisterSequenceHit(Collider2D target)
    {
    }

    private void CleanupSequenceState()
    {
        playerReferences?.Combat?.CancelAllAttacks();
        //playerReferences?.WeaponOverride?.ClearActiveOverride();
        playerReferences?.Input?.EndSequenceInputOverride();
        playerReferences?.WeaponSlots?.RefreshResolvedFireModes(forceRelease: false);
        activeDefinition = null;
    }

    private Vector2 ResolveSequenceDashDirection()
    {
        if (playerReferences != null &&
            playerReferences.Input != null &&
            playerReferences.Input.Move.sqrMagnitude > 0.0001f)
        {
            return playerReferences.Input.Move.normalized;
        }

        if (playerReferences != null &&
            playerReferences.StateMachine != null &&
            playerReferences.StateMachine.LastNonZeroMoveDir.sqrMagnitude > 0.0001f)
        {
            return playerReferences.StateMachine.LastNonZeroMoveDir.normalized;
        }

        if (playerReferences != null &&
            playerReferences.Aim != null &&
            playerReferences.Aim.CurrentAim.sqrMagnitude > 0.0001f)
        {
            return playerReferences.Aim.CurrentAim.normalized;
        }

        return Vector2.right;
    }

    private static TimingJudgement EvaluateTiming(float normalizedTime, TimedSequenceActionRule rule)
    {
        if (rule == null || !rule.Enabled)
            return TimingJudgement.Fail;

        return rule.Evaluate(normalizedTime);
    }
}