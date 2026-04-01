using UnityEngine;

public class ShotgunSequenceController : SequenceControllerBase
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController uiController;
    [SerializeField] private ShotgunSequenceActorAdapter actorAdapterComponent;

    [Header("Sequence Weapon")]
    [SerializeField] private WeaponSlotType targetSlot = WeaponSlotType.Main;

    [Header("Debug")]
    [SerializeField] private bool verboseLogs = false;

    private readonly ShotgunSequencePerformanceTracker tracker = new();

    private int dashResetsUsedInCurrentStep;
    private float sequenceStartTime;

    protected override void Update()
    {
        base.Update();

        if (!runtime.IsRunning || activeDefinition == null)
            return;

        ShotgunSequenceDefinitionSO definition = activeDefinition as ShotgunSequenceDefinitionSO;
        if (definition == null)
            return;

        PlayerInputReader input = playerReferences != null ? playerReferences.Input : null;
        if (input == null)
            return;

        if (definition.UseMaxSequenceDuration)
        {
            float elapsed = Time.time - sequenceStartTime;
            if (elapsed >= definition.MaxSequenceDurationSeconds)
            {
                FailSequence(SequenceFailReason.Timeout);
                return;
            }
        }

        if (runtime.Phase == SequencePhase.Startup)
        {
            UpdateWindowUI(0f);
            return;
        }

        if (runtime.Phase == SequencePhase.StepWindow)
        {
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
        }
    }

    public bool StartSequence(ShotgunSequenceDefinitionSO definition)
    {
        if (definition == null)
            return false;

        ResolveReferences();

        if (actorAdapterComponent == null)
        {
            Debug.LogError("[ShotgunSequenceController] Missing ShotgunSequenceActorAdapter.", this);
            return false;
        }

        bool started = BeginSequenceInternal(definition, tracker, actorAdapterComponent);
        if (!started)
            return false;

        dashResetsUsedInCurrentStep = 0;
        sequenceStartTime = Time.time;

        uiController?.ShowShotgun(definition, playerReferences);

        if (verboseLogs)
            Debug.Log($"[ShotgunSequenceController] Started -> {definition.SequenceId}", this);

        return true;
    }

    public void RegisterPelletHit(Collider2D target, float damage = 0f)
    {
        if (!runtime.IsRunning || target == null)
            return;

        tracker.RegisterPelletHit(target, damage);
    }

    public void NotifySuccessfulDashDuringSequence()
    {
        if (!runtime.IsRunning || runtime.Phase != SequencePhase.StepWindow)
            return;

        ShotgunSequenceDefinitionSO definition = activeDefinition as ShotgunSequenceDefinitionSO;
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
        runtime.OpenCurrentStepWindow();
        playerReferences?.Input?.ClearBufferedInputs();

        if (verboseLogs)
        {
            Debug.Log(
                $"[ShotgunSequenceController] Dash reset current window -> step={runtime.CurrentStepIndex} completed={runtime.CompletedSteps} dashResetsUsed={dashResetsUsedInCurrentStep}",
                this);
        }
    }

    protected override void TickStepWindow()
    {
        base.TickStepWindow();

        if (runtime.Phase == SequencePhase.StepWindow)
            actorAdapter?.EnterStepWindow(runtime.CurrentStepIndex);
    }

    protected override void OnSequenceCompletingStarted()
    {
        tracker.EndSequenceShotIfOpen();
        uiController?.Hide();
    }

    protected override void CompleteSequenceNow()
    {
        // Quita la override temporal de la shotgun base ANTES de que se aplique el reward.
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        base.CompleteSequenceNow();
    }

    protected override void FailSequence(SequenceFailReason reason)
    {
        tracker.EndSequenceShotIfOpen();
        uiController?.Hide();

        // Si la secuencia falla, quitamos la shotgun base temporal.
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        base.FailSequence(reason);
    }

    protected override void CancelSequenceInternal(bool notifyActor = true)
    {
        tracker.EndSequenceShotIfOpen();
        uiController?.Hide();

        // Si la secuencia se cancela, quitamos la shotgun base temporal.
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        base.CancelSequenceInternal(notifyActor);
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

        if (actorAdapterComponent == null)
            actorAdapterComponent = GetComponent<ShotgunSequenceActorAdapter>();

        if (actorAdapterComponent == null)
            actorAdapterComponent = GetComponentInChildren<ShotgunSequenceActorAdapter>(true);

        if (actorAdapterComponent == null)
            actorAdapterComponent = GetComponentInParent<ShotgunSequenceActorAdapter>();
    }

    private void HandlePrimary(float normalizedTime)
    {
        ShotgunSequenceDefinitionSO definition = activeDefinition as ShotgunSequenceDefinitionSO;
        if (definition == null)
            return;

        TimingJudgement predictedJudgement = EvaluateTiming(normalizedTime, definition.ShootRule);

        if (predictedJudgement == TimingJudgement.Fail)
        {
            if (definition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        SequenceActionResult result = actorAdapterComponent.TryHandlePrimaryAction(normalizedTime);
        if (!result.accepted)
        {
            if (definition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        WeaponBehaviour weapon = playerReferences?.WeaponSlots?.GetWeaponBySlot(definition.TargetSlot);
        ShotgunWeaponDataSO shotgunData = weapon != null ? weapon.WeaponData as ShotgunWeaponDataSO : null;

        if (weapon == null || shotgunData == null)
        {
            Debug.LogError("[ShotgunSequenceController] Missing base shotgun weapon in target slot.", this);
            FailSequence(SequenceFailReason.InvalidDefinition);
            return;
        }

        tracker.EndSequenceShotIfOpen();
        tracker.BeginSequenceShot(Mathf.Max(1, shotgunData.pellets));

        bool didFire = weapon.TryFire();
        if (!didFire)
        {
            tracker.EndSequenceShotIfOpen();

            if (definition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        TimingJudgement finalJudgement =
            result.perfect ? TimingJudgement.Perfect :
            result.good ? TimingJudgement.Good :
            TimingJudgement.Good;

        tracker.RegisterActivation(finalJudgement);
        uiController?.FlashJudgement(finalJudgement);

        dashResetsUsedInCurrentStep = 0;
        runtime.MarkCurrentStepCompleted();

        if (verboseLogs)
        {
            Debug.Log(
                $"[ShotgunSequenceController] Step completed -> step={runtime.CurrentStepIndex} completed={runtime.CompletedSteps} pelletsFiredTotal={tracker.PelletsFiredTotal} pelletsHitTotal={tracker.PelletsHitTotal}",
                this);
        }
    }

    private void UpdateWindowUI(float normalizedTime)
    {
        if (uiController == null)
            return;

        ShotgunSequenceDefinitionSO definition = activeDefinition as ShotgunSequenceDefinitionSO;
        if (definition == null)
            return;

        SequenceRewardPreviewInfo previewInfo = BuildRewardPreviewInfo(definition);

        int pelletsRequired = 0;
        if (definition.RewardPolicy is ShotgunConditionalRewardPolicySO shotgunPolicy)
            pelletsRequired = shotgunPolicy.ComputeRequiredPellets(tracker.PelletsFiredTotal);

        SequencePerformanceUISnapshot snapshot = tracker.BuildUISnapshot(
            currentProgress: runtime.CompletedSteps,
            requiredProgress: definition.RequiredSuccessfulSteps,
            rewardEligible: previewInfo.stateText == "READY",
            pelletsRequiredForReward: pelletsRequired);

        snapshot.rewardStateText = previewInfo.stateText;
        snapshot.rewardFormulaText = previewInfo.formulaText;
        snapshot.rewardResultText = previewInfo.resultText;

        uiController.SetShotgunWindowProgress(
            normalizedTime,
            snapshot.currentProgress,
            snapshot.requiredProgress,
            definition);

        uiController.SetPerformanceSnapshot(snapshot);
    }

    private SequenceRewardPreviewInfo BuildRewardPreviewInfo(ShotgunSequenceDefinitionSO definition)
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
}