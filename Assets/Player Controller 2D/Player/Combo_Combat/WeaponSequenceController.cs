using UnityEngine;

public class WeaponSequenceController : MonoBehaviour
{
    private enum RuntimeState
    {
        Inactive,
        Arming,
        WaitingWindow,
        WaitingDashEnd
    }

    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController uiController;
    [SerializeField] private WeaponAimGuideController aimGuideController;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private WeaponSequenceDefinitionSO activeDefinition;
    private WeaponSequencePerformance activePerformance;
    private RuntimeState runtimeState = RuntimeState.Inactive;

    private float armUntilTime;
    private float windowStartTime;
    private float windowEndTime;

    public bool IsSequenceActive => activeDefinition != null;
    public WeaponSequenceDefinitionSO ActiveDefinition => activeDefinition;
    public WeaponSequencePerformance ActivePerformance => activePerformance;

    private void Awake()
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
            Debug.LogWarning("[WeaponSequenceController] Tried to start invalid sequence.", this);
            return false;
        }

        if (playerReferences == null)
        {
            Debug.LogError("[WeaponSequenceController] PlayerReferences missing.", this);
            return false;
        }

        CancelSequenceInternal(clearOverride: false, hideUI: true, hideAimGuide: true, logReason: false);

        activeDefinition = definition;
        activePerformance = new WeaponSequencePerformance();
        runtimeState = RuntimeState.Arming;

        playerReferences.Combat?.CancelAllAttacks();
        playerReferences.Input?.ClearBufferedInputs();
        Debug.Log("[WeaponSequenceController] Clear override from StartSequence", this);
        playerReferences.WeaponOverride?.ClearActiveOverride();
        playerReferences.WeaponOverride?.ClearActiveOverride();

        int initialAmmo = definition.ResolveInitialAmmo();
        playerReferences.WeaponOverride?.ApplyTemporaryWeaponOverride(
            definition.TargetSlot,
            definition.SequenceWeaponData,
            initialAmmo);

        armUntilTime = Time.time + Mathf.Max(0f, definition.StartupDelay);

        uiController?.Show(definition, playerReferences);

        if (definition.ShowAimGuide)
            aimGuideController?.ShowGuide();

        if (debugLogs)
            Debug.Log($"[WeaponSequenceController] Sequence started -> {definition.SequenceId}", this);

        return true;
    }

    public void TickSequence(PlayerInputReader input)
    {
        if (!IsSequenceActive || input == null)
            return;

        if (runtimeState == RuntimeState.Arming)
        {
            uiController?.SetWindowProgress(
                0f,
                activePerformance.SuccessfulShots,
                activeDefinition.RequiredSuccessfulShots,
                activeDefinition);

            if (Time.time >= armUntilTime)
                OpenNewDecisionWindow();

            return;
        }

        if (runtimeState == RuntimeState.WaitingDashEnd)
        {
            uiController?.SetWaitingDashEnd(
                activePerformance.SuccessfulShots,
                activeDefinition.RequiredSuccessfulShots,
                activeDefinition);

            if (!playerReferences.DashController.IsDashing)
            {
                playerReferences.Input?.ClearBufferedInputs();
                OpenNewDecisionWindow();
            }

            return;
        }

        if (runtimeState != RuntimeState.WaitingWindow)
            return;

        float normalizedTime = GetWindowNormalizedTime();

        uiController?.SetWindowProgress(
            normalizedTime,
            activePerformance.SuccessfulShots,
            activeDefinition.RequiredSuccessfulShots,
            activeDefinition);

        if (Time.time >= windowEndTime)
        {
            if (activeDefinition.FailOnTimeout)
            {
                FailSequence("Timeout");
                return;
            }

            OpenNewDecisionWindow();
            return;
        }

        if (activeDefinition.FailOnSwitchWeaponInput && input.ConsumeSwitchWeaponPressed())
        {
            FailSequence("Switch weapon is not allowed during sequence.");
            return;
        }

        if (activeDefinition.FailOnSecondaryInput && input.ConsumeFireSecondaryPressed())
        {
            FailSequence("Secondary fire is not allowed during sequence.");
            return;
        }

        if (input.ConsumeFirePrimaryPressed())
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

    public void RegisterSequenceHit(Collider2D target)
    {
        if (!IsSequenceActive || activePerformance == null)
            return;

        activePerformance.RegisterHit(target);
    }

    public void CancelSequence()
    {
        CancelSequenceInternal(clearOverride: true, hideUI: true, hideAimGuide: true, logReason: true);
    }

    private void HandleShootInput(float normalizedTime)
    {
        if (activeDefinition == null || !activeDefinition.ShootRule.Enabled)
        {
            FailSequence("Shoot action is disabled.");
            return;
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponSequenceController] Shoot input debug -> {activeDefinition.ShootRule.GetDebugSummary(normalizedTime)}",
                this);
        }

        TimingJudgement judgement = activeDefinition.ShootRule.Evaluate(normalizedTime);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponSequenceController] Shoot judgement -> {judgement}",
                this);
        }

        if (judgement == TimingJudgement.Fail)
        {
            FailSequence("Shoot timing failed.");
            return;
        }

        bool didFire = playerReferences.WeaponSlots != null && playerReferences.WeaponSlots.FirePrimary();
        if (!didFire)
        {
            FailSequence("Weapon failed to fire.");
            return;
        }

        activePerformance.RegisterShot(judgement);
        uiController?.FlashJudgement(judgement);
        aimGuideController?.FlashShot();

        if (debugLogs)
            Debug.Log($"[WeaponSequenceController] Shoot success -> {judgement}", this);

        if (activePerformance.SuccessfulShots >= activeDefinition.RequiredSuccessfulShots)
        {
            CompleteSequence();
            return;
        }

        playerReferences.Input?.ClearBufferedInputs();
        OpenNewDecisionWindow();
    }

    private void HandleDashInput(float normalizedTime)
    {
        if (activeDefinition == null || !activeDefinition.DashRule.Enabled)
        {
            FailSequence("Dash action is disabled.");
            return;
        }

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponSequenceController] Dash input debug -> {activeDefinition.DashRule.GetDebugSummary(normalizedTime)}",
                this);
        }

        TimingJudgement judgement = activeDefinition.DashRule.Evaluate(normalizedTime);

        if (debugLogs)
        {
            Debug.Log(
                $"[WeaponSequenceController] Dash judgement -> {judgement}",
                this);
        }

        if (judgement == TimingJudgement.Fail)
        {
            FailSequence("Dash timing failed.");
            return;
        }

        Vector2 dashDir = ResolveSequenceDashDirection();

        bool didDash = playerReferences.StateMachine != null &&
                       playerReferences.StateMachine.TryDash(
                           dashDir,
                           ignoreCooldown: true,
                           recordAction: false);

        if (!didDash)
        {
            FailSequence("Dash could not start.");
            return;
        }

        activePerformance.RegisterDash(judgement);
        runtimeState = RuntimeState.WaitingDashEnd;
        uiController?.FlashJudgement(judgement);

        if (debugLogs)
            Debug.Log($"[WeaponSequenceController] Dash success -> {judgement}", this);
    }

    private Vector2 ResolveSequenceDashDirection()
    {
        if (playerReferences.Input != null && playerReferences.Input.Move.sqrMagnitude > 0.0001f)
            return playerReferences.Input.Move.normalized;

        if (playerReferences.StateMachine != null && playerReferences.StateMachine.LastNonZeroMoveDir.sqrMagnitude > 0.0001f)
            return playerReferences.StateMachine.LastNonZeroMoveDir;

        if (playerReferences.Aim != null && playerReferences.Aim.CurrentAim.sqrMagnitude > 0.0001f)
            return playerReferences.Aim.CurrentAim.normalized;

        return Vector2.right;
    }

    private void OpenNewDecisionWindow()
    {
        runtimeState = RuntimeState.WaitingWindow;
        windowStartTime = Time.time;
        windowEndTime = windowStartTime + activeDefinition.DecisionWindowDuration;

        uiController?.SetWindowProgress(
            0f,
            activePerformance.SuccessfulShots,
            activeDefinition.RequiredSuccessfulShots,
            activeDefinition);

        if (debugLogs)
            Debug.Log("[WeaponSequenceController] New decision window opened.", this);
    }

    private float GetWindowNormalizedTime()
    {
        if (windowEndTime <= windowStartTime)
            return 1f;

        return Mathf.InverseLerp(windowStartTime, windowEndTime, Time.time);
    }

    private void CompleteSequence()
    {
        if (debugLogs)
            Debug.Log($"[WeaponSequenceController] Sequence completed -> {activeDefinition.SequenceId}", this);

        WeaponSequenceRewardContext rewardContext = new WeaponSequenceRewardContext(
            activeDefinition,
            activePerformance,
            playerReferences);

        playerReferences.Combat?.CancelAllAttacks();
        Debug.Log("[WeaponSequenceController] Clear override from CompleteSequence", this);
        playerReferences.WeaponOverride?.ClearActiveOverride();
        playerReferences.WeaponOverride?.ClearActiveOverride();
        uiController?.Hide();
        aimGuideController?.HideGuide();

        SequenceRewardSO reward = activeDefinition.CompletionReward;

        activeDefinition = null;
        activePerformance = null;
        runtimeState = RuntimeState.Inactive;
        armUntilTime = 0f;
        windowStartTime = 0f;
        windowEndTime = 0f;

        reward?.Apply(rewardContext);
    }

    private void FailSequence(string reason)
    {
        if (debugLogs)
            Debug.LogWarning($"[WeaponSequenceController] Sequence failed -> {reason}", this);

        playerReferences.Combat?.CancelAllAttacks();
        CancelSequenceInternal(clearOverride: true, hideUI: true, hideAimGuide: true, logReason: false);
    }

    private void CancelSequenceInternal(bool clearOverride, bool hideUI, bool hideAimGuide, bool logReason)
    {
        if (logReason && debugLogs && activeDefinition != null)
            Debug.Log($"[WeaponSequenceController] Sequence cancelled -> {activeDefinition.SequenceId}", this);

        if (clearOverride)
            Debug.Log("[WeaponSequenceController] Clear override from CancelSequenceInternal", this);
        playerReferences.WeaponOverride?.ClearActiveOverride();
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        if (hideUI)
            uiController?.Hide();

        if (hideAimGuide)
            aimGuideController?.HideGuide();

        activeDefinition = null;
        activePerformance = null;
        runtimeState = RuntimeState.Inactive;
        armUntilTime = 0f;
        windowStartTime = 0f;
        windowEndTime = 0f;
    }
}