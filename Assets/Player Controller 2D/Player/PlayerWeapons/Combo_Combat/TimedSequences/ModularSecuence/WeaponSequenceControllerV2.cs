using UnityEngine;

public class WeaponSequenceControllerV2 : SequenceControllerBase
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController uiController;
    [SerializeField] private WeaponAimGuideController aimGuideController;
    [SerializeField] private WeaponSequenceActorAdapter actorAdapterComponent;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private readonly WeaponSequencePerformanceTracker tracker = new();

    protected override void Update()
    {
        base.Update();

        if (!runtime.IsRunning || activeDefinition == null)
            return;

        WeaponSequenceDefinitionSO definition = activeDefinition as WeaponSequenceDefinitionSO;
        if (definition == null)
            return;

        PlayerInputReader input = playerReferences != null ? playerReferences.Input : null;
        if (input == null)
            return;

        if (runtime.Phase == SequencePhase.Startup)
        {
            UpdateWindowUI(0f);
            return;
        }

        if (runtime.Phase != SequencePhase.StepWindow)
            return;

        float normalizedTime = runtime.GetWindowNormalizedTime();
        UpdateWindowUI(normalizedTime);

        if (definition.FailOnSwitchWeaponInput && input.ConsumeSwitchWeaponPressed())
        {
            FailSequence(SequenceFailReason.ForbiddenInput);
            return;
        }

        if (definition.FailOnSecondaryInput && input.ConsumeSecondaryFireRequest())
        {
            FailSequence(SequenceFailReason.ForbiddenInput);
            return;
        }

        if (input.ConsumePrimaryFireRequest())
        {
            HandlePrimary(normalizedTime);
            return;
        }

        if (input.ConsumeDashPressed())
        {
            HandleDash(normalizedTime);
            return;
        }
    }

    public bool StartSequence(WeaponSequenceDefinitionSO definition)
    {
        if (definition == null)
            return false;

        ResolveReferences();

        if (actorAdapterComponent == null)
        {
            Debug.LogError("[WeaponSequenceControllerV2] Missing WeaponSequenceActorAdapter.", this);
            return false;
        }

        bool started = BeginSequenceInternal(definition, tracker, actorAdapterComponent);
        if (!started)
            return false;

        uiController?.Show(definition, playerReferences);

        if (definition.ShowAimGuide)
            aimGuideController?.ShowGuide();

        if (verboseLogs)
            Debug.Log($"[WeaponSequenceControllerV2] Started -> {definition.SequenceId}", this);

        return true;
    }

    public void RegisterSequenceHit(Collider2D target)
    {
        if (!runtime.IsRunning || target == null)
            return;

        tracker.RegisterShotHit(target, 0f);
    }

    protected override void TickStepWindow()
    {
        base.TickStepWindow();

        if (runtime.Phase == SequencePhase.StepWindow)
            actorAdapter?.EnterStepWindow(runtime.CurrentStepIndex);
    }

    protected override void CompleteSequenceNow()
    {
        tracker.EndShotIfOpen();

        uiController?.Hide();
        aimGuideController?.HideGuide();
        base.CompleteSequenceNow();
    }

    protected override void FailSequence(SequenceFailReason reason)
    {
        tracker.EndShotIfOpen();

        uiController?.Hide();
        aimGuideController?.HideGuide();
        base.FailSequence(reason);
    }

    protected override void CancelSequenceInternal(bool notifyActor = true)
    {
        tracker.EndShotIfOpen();

        uiController?.Hide();
        aimGuideController?.HideGuide();
        base.CancelSequenceInternal(notifyActor);
    }

    protected override void HandleActionResult(SequenceActionResult result)
    {
        base.HandleActionResult(result);

        if (result.accepted)
            uiController?.FlashJudgement(result.perfect ? TimingJudgement.Perfect : TimingJudgement.Good);
    }

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

        if (actorAdapterComponent == null)
            actorAdapterComponent = GetComponent<WeaponSequenceActorAdapter>();

        if (actorAdapterComponent == null)
            actorAdapterComponent = GetComponentInChildren<WeaponSequenceActorAdapter>(true);

        if (actorAdapterComponent == null)
            actorAdapterComponent = GetComponentInParent<WeaponSequenceActorAdapter>();
    }

    private void HandlePrimary(float normalizedTime)
    {
        WeaponSequenceDefinitionSO definition = activeDefinition as WeaponSequenceDefinitionSO;
        if (definition == null)
            return;

        tracker.EndShotIfOpen();

        TimingJudgement predictedJudgement =
            WeaponSequenceInputEvaluator.EvaluateShoot(definition, normalizedTime);

        if (predictedJudgement == TimingJudgement.Fail)
        {
            if (definition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        tracker.BeginShot(predictedJudgement);

        SequenceActionResult result = actorAdapter.TryHandlePrimaryAction(normalizedTime);
        if (!result.accepted)
        {
            tracker.EndShotIfOpen();

            if (definition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        aimGuideController?.FlashShot();

        runtime.MarkCurrentStepCompleted();

        if (runtime.Phase == SequencePhase.Completed)
        {
            CompleteSequenceNow();
            return;
        }

        if (runtime.IsRunning && runtime.Phase == SequencePhase.StepWindow)
            playerReferences?.Input?.ClearBufferedInputs();
    }

    private void HandleDash(float normalizedTime)
    {
        WeaponSequenceDefinitionSO definition = activeDefinition as WeaponSequenceDefinitionSO;
        if (definition == null)
            return;

        SequenceActionResult result = actorAdapter.TryHandleDashAction(normalizedTime);
        if (!result.accepted)
        {
            if (definition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        tracker.RegisterDash(result.perfect ? TimingJudgement.Perfect : TimingJudgement.Good);
        runtime.MarkCurrentStepCompleted();

        if (runtime.Phase == SequencePhase.Completed)
        {
            CompleteSequenceNow();
            return;
        }

        if (runtime.IsRunning && runtime.Phase == SequencePhase.StepWindow)
            playerReferences?.Input?.ClearBufferedInputs();
    }



    private void UpdateWindowUI(float normalizedTime)
    {
        if (uiController == null)
            return;

        WeaponSequenceDefinitionSO definition = activeDefinition as WeaponSequenceDefinitionSO;
        if (definition == null)
            return;

        SequenceRewardPreviewInfo previewInfo = BuildRewardPreviewInfo(definition);

        SequencePerformanceUISnapshot snapshot = tracker.BuildUISnapshot(
            currentProgress: tracker.SuccessfulActions,
            requiredProgress: definition.RequiredSuccessfulShots,
            rewardEligible: previewInfo.stateText == "READY");

        snapshot.rewardStateText = previewInfo.stateText;
        snapshot.rewardFormulaText = previewInfo.formulaText;
        snapshot.rewardResultText = previewInfo.resultText;

        uiController.SetWindowProgress(
            normalizedTime,
            snapshot.currentProgress,
            snapshot.requiredProgress,
            definition);

        uiController.SetPerformanceSnapshot(snapshot);
    }

    private SequenceRewardPreviewInfo BuildRewardPreviewInfo(WeaponSequenceDefinitionSO definition)
    {
        if (definition == null || definition.RewardPolicy == null)
            return SequenceRewardPreviewInfo.Empty;

        SequenceRewardContextBase previewContext = tracker.BuildRewardContext(
            sequenceCompleted: true,
            completedSteps: runtime.CompletedSteps,
            attemptedSteps: runtime.CurrentStepIndex + 1);

        SequenceRewardResolution previewResolution =
            definition.RewardPolicy.Evaluate(previewContext, definition);

        return definition.RewardPolicy.BuildPreview(previewContext, definition, previewResolution);
    }

    private bool EvaluateRewardEligibilityPreview(WeaponSequenceDefinitionSO definition)
    {
        if (definition == null || definition.RewardPolicy == null)
            return false;

        SequenceRewardContextBase previewContext = tracker.BuildRewardContext(
            sequenceCompleted: true,
            completedSteps: runtime.CompletedSteps,
            attemptedSteps: runtime.CurrentStepIndex + 1);

        SequenceRewardResolution preview =
            definition.RewardPolicy.Evaluate(previewContext, definition);

        return preview.shouldApply;
    }
}