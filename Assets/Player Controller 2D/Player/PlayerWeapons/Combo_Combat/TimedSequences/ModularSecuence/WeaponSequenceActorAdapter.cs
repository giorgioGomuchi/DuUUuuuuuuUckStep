using System;
using UnityEngine;

public class WeaponSequenceActorAdapter : MonoBehaviour, ISequenceActorAdapter
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;

    [Header("Input During Sequence")]
    [SerializeField] private bool forcePrimarySinglePress = true;
    [SerializeField] private bool forceSecondarySinglePress = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private WeaponSequenceDefinitionSO activeDefinition;

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

    public void Bind()
    {
    }

    public void Unbind()
    {
    }

    public void OnSequenceStarted(SequenceDefinitionSOBase definition)
    {
        activeDefinition = definition as WeaponSequenceDefinitionSO;

        if (activeDefinition == null)
        {
            OnExternalSequenceFail?.Invoke(SequenceFailReason.InvalidDefinition);
            return;
        }

        playerReferences?.Combat?.CancelAllAttacks();

        playerReferences?.Input?.BeginSequenceInputOverride(
            forcePrimarySinglePress,
            forceSecondarySinglePress);

        playerReferences?.WeaponOverride?.ClearActiveOverride();

        int ammoCount = activeDefinition.ResolveInitialAmmo();

        playerReferences?.WeaponOverride?.ApplyTemporaryWeaponOverride(
            activeDefinition.TargetSlot,
            activeDefinition.SequenceWeaponData,
            ammoCount);

        if (debugLogs)
            Debug.Log($"[WeaponSequenceActorAdapter] Started -> {activeDefinition.SequenceId}", this);
    }

    public void OnSequenceCancelled()
    {
        CleanupSequenceState();
    }

    public void OnSequenceFailed(SequenceFailReason reason)
    {
        CleanupSequenceState();

        if (debugLogs)
            Debug.LogWarning($"[WeaponSequenceActorAdapter] Failed -> {reason}", this);
    }

    public void OnSequenceCompleted()
    {
        playerReferences?.Combat?.CancelAllAttacks();
        playerReferences?.Input?.EndSequenceInputOverride();
        playerReferences?.WeaponSlots?.RefreshResolvedFireModes(forceRelease: false);

        if (debugLogs && activeDefinition != null)
            Debug.Log($"[WeaponSequenceActorAdapter] Completed -> {activeDefinition.SequenceId}", this);

        activeDefinition = null;
    }

    public void EnterStepWindow(int stepIndex)
    {
    }

    public void ExitStepWindow(int stepIndex)
    {
    }

    public void TickSequence(float deltaTime)
    {
    }

    public SequenceActionResult TryHandlePrimaryAction(float normalizedWindowTime)
    {
        if (activeDefinition == null)
            return SequenceActionResult.Rejected;

        TimingJudgement judgement =
            WeaponSequenceInputEvaluator.EvaluateShoot(activeDefinition, normalizedWindowTime);

        if (judgement == TimingJudgement.Fail)
            return SequenceActionResult.Rejected;

        bool didFire =
            playerReferences != null &&
            playerReferences.WeaponSlots != null &&
            playerReferences.WeaponSlots.FirePrimary();

        if (!didFire)
            return SequenceActionResult.Rejected;

        playerReferences?.Input?.ClearBufferedInputs();

        return new SequenceActionResult
        {
            accepted = true,
            completedStep = true,
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

        TimingJudgement judgement =
            WeaponSequenceInputEvaluator.EvaluateDash(activeDefinition, normalizedWindowTime);

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
            completedStep = true,
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
        // Hook opcional para forward externo si lo necesitas luego.
    }

    private void CleanupSequenceState()
    {
        playerReferences?.Combat?.CancelAllAttacks();
        playerReferences?.WeaponOverride?.ClearActiveOverride();
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
}