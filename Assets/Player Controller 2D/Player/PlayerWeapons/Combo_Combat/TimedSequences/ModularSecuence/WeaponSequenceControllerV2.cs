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
    private int dashResetsUsedInCurrentStep;
   
    //OJO que aqui estamos metiendo timpo para limitar el tiempo de la secuencia cuidao que igual sobra
    private float sequenceStartTime;

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

        if (definition.UseMaxSequenceDuration)
        {
            float elapsed = Time.time - sequenceStartTime;
            if (elapsed >= definition.MaxSequenceDurationSeconds)
            {
                FailSequence(SequenceFailReason.Timeout);
                return;
            }
        }

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

        //OJO que aqui estamos metiendo timpo para limitar el tiempo de la secuencia cuidao que igual sobra
        dashResetsUsedInCurrentStep = 0;
        sequenceStartTime = Time.time;

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

    public void NotifySuccessfulDashDuringSequence()
    {
        if (!runtime.IsRunning || runtime.Phase != SequencePhase.StepWindow)
            return;

        WeaponSequenceDefinitionSO definition = activeDefinition as WeaponSequenceDefinitionSO;
        if (definition == null || !definition.DashRule.Enabled)
            return;

        float normalizedTime = runtime.GetWindowNormalizedTime();
        TimingJudgement judgement = EvaluateTiming(normalizedTime, definition.DashRule);

        if (judgement == TimingJudgement.Fail)
        {
            if (definition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        if (definition.UseDashResetLimitPerStep &&
            dashResetsUsedInCurrentStep >= definition.MaxDashResetsPerCurrentStep)
        {
            if (definition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        tracker.RegisterDash(judgement);
        dashResetsUsedInCurrentStep++;

        uiController?.FlashJudgement(judgement);

        // Reinicia solo la ventana del step actual sin perder el progreso ya conseguido.
        runtime.OpenCurrentStepWindow();
        playerReferences?.Input?.ClearBufferedInputs();

        if (verboseLogs)
        {
            Debug.Log(
                $"[WeaponSequenceControllerV2] Dash reset current shot window -> step={runtime.CurrentStepIndex} completed={runtime.CompletedSteps} dashResetsUsed={dashResetsUsedInCurrentStep}",
                this);
        }
    }

    private static TimingJudgement EvaluateTiming(float normalizedTime, TimedSequenceActionRule rule)
    {
        if (rule == null || !rule.Enabled)
            return TimingJudgement.Fail;

        float center = 0.5f;
        float distance = Mathf.Abs(normalizedTime - center);

        if (rule.AllowPerfect && distance <= rule.PerfectHalfWindowNormalized)
            return TimingJudgement.Perfect;

        if (distance <= rule.GoodHalfWindowNormalized)
            return TimingJudgement.Good;

        return TimingJudgement.Fail;
    }

    protected override void TickStepWindow()
    {
        base.TickStepWindow();

        if (runtime.Phase == SequencePhase.StepWindow)
            actorAdapter?.EnterStepWindow(runtime.CurrentStepIndex);
    }

    protected override void CompleteSequenceNow()
    {

        base.CompleteSequenceNow();
    }

    protected override void OnSequenceCompletingStarted()
    {
        tracker.EndShotIfOpen();
        uiController?.Hide();
        aimGuideController?.HideGuide();
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

        dashResetsUsedInCurrentStep = 0;

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
            currentProgress: runtime.CompletedSteps,
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

}