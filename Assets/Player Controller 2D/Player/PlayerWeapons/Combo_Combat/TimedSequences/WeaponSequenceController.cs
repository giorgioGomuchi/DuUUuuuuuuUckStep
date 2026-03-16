using UnityEngine;

/// Controls sequence lifecycle, delegates state/timing to WeaponSequenceRuntime,
/// and keeps UI / aim guide / reward orchestration in one place.
public class WeaponSequenceController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController uiController;
    [SerializeField] private WeaponAimGuideController aimGuideController;

    [Header("Input During Sequence")]
    [SerializeField] private bool forcePrimarySinglePress = true;
    [SerializeField] private bool forceSecondarySinglePress = false;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private readonly WeaponSequenceRuntime runtime = new WeaponSequenceRuntime();

    public bool IsSequenceActive => runtime.IsActive;
    public WeaponSequenceDefinitionSO ActiveDefinition => runtime.ActiveDefinition;
    public WeaponSequencePerformance ActivePerformance => runtime.Performance;

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

        if (uiController == null)
            uiController = GetComponentInChildren<TimedSequenceUIController>(true);

        if (aimGuideController == null)
            aimGuideController = GetComponentInChildren<WeaponAimGuideController>(true);
    }

    public bool StartSequence(WeaponSequenceDefinitionSO definition)
    {
        if (definition == null || !definition.IsValid())
        {
            Debug.LogWarning("[WeaponSequenceController] Invalid sequence definition.", this);
            return false;
        }

        if (playerReferences == null)
        {
            Debug.LogError("[WeaponSequenceController] Missing PlayerReferences.", this);
            return false;
        }

        CancelSequenceInternal(
            clearOverride: false,
            hideUI: true,
            hideAimGuide: true,
            restoreInputState: true,
            logIfActive: false);

        runtime.Begin(definition);

        playerReferences.Combat?.CancelAllAttacks();

        PrepareSequenceInput();
        PrepareSequenceWeapon();

        uiController?.Show(definition, playerReferences);

        if (definition.ShowAimGuide)
            aimGuideController?.ShowGuide();

        if (debugLogs)
            Debug.Log($"[WeaponSequenceController] Sequence started -> {definition.SequenceId}", this);

        return true;
    }

    public void TickSequence(PlayerInputReader input)
    {
        if (!runtime.IsActive || input == null)
            return;

        switch (runtime.State)
        {
            case WeaponSequenceRuntimeState.Arming:
                TickArming();
                break;

            case WeaponSequenceRuntimeState.WaitingWindow:
                TickWaitingWindow(input);
                break;

            case WeaponSequenceRuntimeState.WaitingDashEnd:
                TickWaitingDashEnd();
                break;

            case WeaponSequenceRuntimeState.Completing:
                TickCompleting();
                break;
        }
    }

    public void RegisterSequenceHit(Collider2D target)
    {
        if (!runtime.IsActive || runtime.Performance == null)
            return;

        runtime.Performance.RegisterHit(target);
    }

    public void CancelSequence()
    {
        CancelSequenceInternal(
            clearOverride: true,
            hideUI: true,
            hideAimGuide: true,
            restoreInputState: true,
            logIfActive: true);
    }

    private void TickArming()
    {
        UpdateWindowUI(0f);

        if (!runtime.IsArmingComplete())
            return;

        runtime.OpenDecisionWindow();

        if (debugLogs)
            Debug.Log("[WeaponSequenceController] Sequence armed -> decision window opened.", this);
    }

    private void TickWaitingWindow(PlayerInputReader input)
    {
        float normalizedTime = runtime.GetWindowNormalizedTime();
        UpdateWindowUI(normalizedTime);

        if (runtime.IsDecisionWindowExpired())
        {
            if (WeaponSequenceInputEvaluator.ShouldFailOnTimeout(runtime.ActiveDefinition))
            {
                FailSequence("Timeout.");
                return;
            }

            runtime.OpenDecisionWindow();
            return;
        }

        if (WeaponSequenceInputEvaluator.IsSwitchForbidden(runtime.ActiveDefinition) &&
            input.ConsumeSwitchWeaponPressed())
        {
            FailSequence("Switch weapon is not allowed during sequence.");
            return;
        }

        if (WeaponSequenceInputEvaluator.IsSecondaryFireForbidden(runtime.ActiveDefinition) &&
            input.ConsumeSecondaryFireRequest())
        {
            FailSequence("Secondary fire is not allowed during sequence.");
            return;
        }

        if (input.ConsumePrimaryFireRequest())
        {
            HandleShootInput(normalizedTime);
            return;
        }

        if (input.ConsumeDashPressed())
        {
            HandleDashInput(normalizedTime);
            return;
        }
    }

    private void TickWaitingDashEnd()
    {
        uiController?.SetWaitingDashEnd(
            runtime.Performance.SuccessfulShots,
            runtime.ActiveDefinition.RequiredSuccessfulShots,
            runtime.ActiveDefinition);

        if (playerReferences == null ||
            playerReferences.DashController == null ||
            playerReferences.DashController.IsDashing)
        {
            return;
        }

        playerReferences.Input?.ClearBufferedInputs();
        runtime.OpenDecisionWindow();
    }

    private void TickCompleting()
    {
        UpdateWindowUI(1f);

        if (!runtime.IsCompletionReady())
            return;

        CompleteSequenceNow();
    }

    private void HandleShootInput(float normalizedTime)
    {
        TimingJudgement judgement =
            WeaponSequenceInputEvaluator.EvaluateShoot(runtime.ActiveDefinition, normalizedTime);

        if (debugLogs)
            Debug.Log($"[WeaponSequenceController] Shoot judgement -> {judgement}", this);

        if (judgement == TimingJudgement.Fail)
        {
            FailSequence("Shoot timing failed.");
            return;
        }

        bool didFire = playerReferences != null &&
                       playerReferences.WeaponSlots != null &&
                       playerReferences.WeaponSlots.FirePrimary();

        if (!didFire)
        {
            FailSequence("Weapon failed to fire.");
            return;
        }

        runtime.Performance.RegisterShot(judgement);

        uiController?.FlashJudgement(judgement);
        aimGuideController?.FlashShot();

        if (runtime.Performance.SuccessfulShots >= runtime.ActiveDefinition.RequiredSuccessfulShots)
        {
            QueueCompletion();
            return;
        }

        playerReferences.Input?.ClearBufferedInputs();
        runtime.OpenDecisionWindow();
    }

    private void HandleDashInput(float normalizedTime)
    {
        TimingJudgement judgement =
            WeaponSequenceInputEvaluator.EvaluateDash(runtime.ActiveDefinition, normalizedTime);

        if (debugLogs)
            Debug.Log($"[WeaponSequenceController] Dash judgement -> {judgement}", this);

        if (judgement == TimingJudgement.Fail)
        {
            FailSequence("Dash timing failed.");
            return;
        }

        Vector2 dashDirection = ResolveSequenceDashDirection();

        bool didDash = playerReferences != null &&
                       playerReferences.StateMachine != null &&
                       playerReferences.StateMachine.TryDash(
                           dashDirection,
                           ignoreCooldown: true,
                           recordAction: false);

        if (!didDash)
        {
            FailSequence("Dash could not start.");
            return;
        }

        runtime.Performance.RegisterDash(judgement);
        runtime.EnterWaitingDashEnd();
        uiController?.FlashJudgement(judgement);
    }

    private void QueueCompletion()
    {
        runtime.QueueCompletion();

        // Prevent reward weapon from using the same press / hold.
        if (playerReferences != null && playerReferences.Input != null)
        {
            playerReferences.Input.ForceReleaseFireInputs();
            playerReferences.Input.ClearBufferedInputs();
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponSequenceController] Completion queued -> executeAt={runtime.CompletionAtTime:F3}",
                this);
        }
    }

    private void CompleteSequenceNow()
    {
        if (!runtime.IsActive)
            return;

        WeaponSequenceDefinitionSO completedDefinition = runtime.ActiveDefinition;
        WeaponSequencePerformance completedPerformance = runtime.Performance;
        SequenceRewardSO reward = completedDefinition.CompletionReward;

        if (debugLogs)
            Debug.Log($"[WeaponSequenceController] Sequence completed -> {completedDefinition.SequenceId}", this);

        WeaponSequenceRewardContext rewardContext = new WeaponSequenceRewardContext(
            completedDefinition,
            completedPerformance,
            playerReferences);

        playerReferences?.Combat?.CancelAllAttacks();
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        uiController?.Hide();
        aimGuideController?.HideGuide();

        RestorePostSequenceInput();

        runtime.Reset();

        reward?.Apply(rewardContext);
    }

    private void PrepareSequenceInput()
    {
        playerReferences?.Input?.BeginSequenceInputOverride(
            forcePrimarySinglePress,
            forceSecondarySinglePress);
    }

    private void PrepareSequenceWeapon()
    {
        if (playerReferences == null || playerReferences.WeaponOverride == null)
            return;

        playerReferences.WeaponOverride.ClearActiveOverride();

        int ammoCount = runtime.ActiveDefinition.ResolveInitialAmmo();

        playerReferences.WeaponOverride.ApplyTemporaryWeaponOverride(
            runtime.ActiveDefinition.TargetSlot,
            runtime.ActiveDefinition.SequenceWeaponData,
            ammoCount);
    }

    private void RestorePostSequenceInput()
    {
        playerReferences?.Input?.EndSequenceInputOverride();
        playerReferences?.WeaponSlots?.RefreshResolvedFireModes(forceRelease: false);
    }

    private void FailSequence(string reason)
    {
        if (debugLogs)
            Debug.LogWarning($"[WeaponSequenceController] Sequence failed -> {reason}", this);

        playerReferences?.Combat?.CancelAllAttacks();

        CancelSequenceInternal(
            clearOverride: true,
            hideUI: true,
            hideAimGuide: true,
            restoreInputState: true,
            logIfActive: false);
    }

    private void CancelSequenceInternal(
        bool clearOverride,
        bool hideUI,
        bool hideAimGuide,
        bool restoreInputState,
        bool logIfActive)
    {
        if (logIfActive && runtime.IsActive && debugLogs)
            Debug.Log($"[WeaponSequenceController] Sequence cancelled -> {runtime.ActiveDefinition.SequenceId}", this);

        if (clearOverride)
            playerReferences?.WeaponOverride?.ClearActiveOverride();

        if (restoreInputState)
            RestorePostSequenceInput();

        if (hideUI)
            uiController?.Hide();

        if (hideAimGuide)
            aimGuideController?.HideGuide();

        runtime.Reset();
    }

    private void UpdateWindowUI(float normalizedTime)
    {
        if (!runtime.IsActive || uiController == null)
            return;

        uiController.SetWindowProgress(
            normalizedTime,
            runtime.Performance.SuccessfulShots,
            runtime.ActiveDefinition.RequiredSuccessfulShots,
            runtime.ActiveDefinition);
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