using UnityEngine;

public class BoomerangSequenceController : MonoBehaviour, IBoomerangSequenceBridge
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController sequenceUI;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Runtime")]
    [SerializeField] private BoomerangSequenceRuntime runtime = new();
    [SerializeField] private BoomerangSequencePendingTransition pendingTransition;
    [SerializeField] private BoomerangSequencePerformanceTracker performanceTracker = new();

    private BoomerangProjectile2D activeProjectile;
    private WeaponBehaviour activeWeapon;
    private BoomerangWeaponDataSO activeWeaponData;
    private BoomerangSequenceDefinitionSO activeDefinition;
    private bool orbitRewardActive;
    private BoomerangSequenceActorAdapter activeActorAdapter;

    private BoomerangSequenceRewardEvaluator rewardEvaluator;
    private BoomerangSequenceUIPresenter uiPresenter;

    public bool IsSequenceActive => runtime.IsRunning;
    public bool IsInOrbitReward => orbitRewardActive;
    public BoomerangSequencePhase ActivePhase => runtime.Phase;
    public int CompletedCycles => runtime.CompletedCycles;
    public BoomerangSequencePerformance Performance => performanceTracker != null ? performanceTracker.Performance : null;

    private void Awake()
    {
        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();

        if (sequenceUI == null)
            sequenceUI = GetComponentInChildren<TimedSequenceUIController>(true);

        rewardEvaluator = new BoomerangSequenceRewardEvaluator();
        uiPresenter = new BoomerangSequenceUIPresenter(sequenceUI);

        pendingTransition.Clear();
    }

    public bool BeginSequence(BoomerangProjectile2D projectile, WeaponBehaviour weapon, BoomerangWeaponDataSO weaponData)
    {
        if (projectile == null || weapon == null || weaponData == null || weaponData.sequenceDefinition == null)
            return false;

        if (!weaponData.sequenceDefinition.IsValid())
            return false;

        CancelActiveSequence(clearOverride: false, destroyProjectile: false);

        activeProjectile = projectile;

        activeActorAdapter = activeProjectile.GetComponent<BoomerangSequenceActorAdapter>();
        if (activeActorAdapter == null)
            activeActorAdapter = activeProjectile.gameObject.AddComponent<BoomerangSequenceActorAdapter>();

        activeActorAdapter.SetProjectile(activeProjectile);

        activeWeapon = weapon;
        activeWeaponData = weaponData;
        activeDefinition = weaponData.sequenceDefinition;
        orbitRewardActive = false;

        activeProjectile.SetSequenceBridge(this);
        BindProjectileEvents(activeProjectile);

        runtime.Reset();
        runtime.BeginRecallWindow(activeDefinition.RecallWindowDuration);
        pendingTransition.Clear();

        performanceTracker.ResetSequence();
        performanceTracker.BeginCycle(1);

        uiPresenter?.Show(activeDefinition, playerReferences);
        UpdateWindowUI();

        if (debugLogs)
            Debug.Log("[BoomerangSequenceController] Sequence started.", this);

        return true;
    }

    public void TickSequence(PlayerInputReader input)
    {
        if (!runtime.IsRunning || input == null || activeDefinition == null)
            return;

        ProcessPendingTransition();

        if (!runtime.IsRunning)
            return;

        if (orbitRewardActive)
            return;

        if (activeDefinition.FailOnSwitchWeaponInput && input.ConsumeSwitchWeaponPressed())
        {
            FailSequence("Switch weapon while boomerang sequence is active.");
            return;
        }

        switch (runtime.Phase)
        {
            case BoomerangSequencePhase.OutboundRecallWindow:
                TickRecallWindow(input);
                break;

            case BoomerangSequencePhase.ReturningToReflectZone:
                TickReturningToReflectZone(input);
                break;

            case BoomerangSequencePhase.ReflectWindow:
                TickReflectWindow(input);
                break;
        }
    }

    public bool TryResolveMeleeReflect(BoomerangProjectile2D projectile, DeflectInfo info)
    {
        if (!runtime.IsRunning || activeDefinition == null)
            return false;

        if (projectile == null || projectile != activeProjectile)
            return false;

        if (runtime.Phase != BoomerangSequencePhase.ReflectWindow)
            return false;

        if (pendingTransition.IsActive)
            return true;

        TimingJudgement judgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.ReflectRule);

        uiPresenter?.FlashJudgement(judgement);

        if (!IsSuccess(judgement))
        {
            projectile.EnterDriftLost();
            FailSequence("Melee reflect outside reflect rule.");
            return true;
        }

        if (runtime.ReflectDashSucceededThisWindow && activeWeaponData != null)
        {
            projectile.ApplyNextReflectDashBoost(
                activeWeaponData.DashReflectSpeedMultiplierBonus);
        }

        ForceWindowUIToEnd(activeDefinition.ReflectRule, "Reflect", false);
        pendingTransition.SetResolveReflect(activeDefinition.UiPhaseTransitionHoldDuration, info.newDirection);
        return true;
    }

    public void RegisterBoomerangDamage(BoomerangProjectile2D projectile, Collider2D other, BoomerangFlightState flightState)
    {
        if (projectile == null || other == null)
            return;

        if (projectile != activeProjectile)
            return;

        if (!runtime.IsRunning && !orbitRewardActive)
            return;

        BoomerangDamageActionType actionType = ResolveDamageActionType(flightState);
        performanceTracker.RegisterDamage(other, actionType);

        if (debugLogs)
        {
            BoomerangSequencePerformance performance = performanceTracker.Performance;
            Debug.Log(
                $"[BoomerangSequenceController] Damage registered action={actionType} totalHits={performance.TotalHitEvents} totalUnique={performance.TotalUniqueEnemiesDamaged} cycle={performance.CurrentCycleNumber} cycleHits={performance.CurrentCycleHitEvents} cycleUnique={performance.CurrentCycleUniqueEnemiesDamaged}",
                this);
        }
    }

    public void CancelActiveSequence(bool clearOverride, bool destroyProjectile)
    {
        pendingTransition.Clear();
        UnbindProjectileEvents();

        if (destroyProjectile && activeProjectile != null)
            Destroy(activeProjectile.gameObject);

        if (clearOverride)
            playerReferences?.WeaponOverride?.ClearActiveOverride();

        uiPresenter?.Hide();

        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;
        orbitRewardActive = false;
        activeActorAdapter = null;

        runtime.Reset();
        performanceTracker.ResetSequence();
    }

    private void ProcessPendingTransition()
    {
        if (!pendingTransition.IsReady())
            return;

        switch (pendingTransition.type)
        {
            case BoomerangPendingTransitionType.BeginReturn:
                ExecuteBeginReturnTransition();
                break;

            case BoomerangPendingTransitionType.ResolveReflect:
                ExecuteResolveReflectTransition();
                break;
        }

        pendingTransition.Clear();
    }

    private void ExecuteBeginReturnTransition()
    {
        if (!runtime.IsRunning || activeProjectile == null || activeDefinition == null)
            return;

        runtime.CompleteRecall();
        runtime.BeginReturnToReflectZone(activeDefinition.ReturnToReflectDuration);

        activeActorAdapter?.BeginReturn(
    activeDefinition.ReturnToReflectDuration,
    activeDefinition.ReflectActivationNormalized);

        UpdateWindowUI();
    }

    private void ExecuteResolveReflectTransition()
    {
        if (!runtime.IsRunning || activeProjectile == null || activeDefinition == null)
            return;

        runtime.CompleteReflect();
        performanceTracker.CommitCurrentCycle();

        activeActorAdapter?.ResolveReflect(pendingTransition.reflectDirection);

        if (runtime.CompletedCycles >= activeDefinition.RequiredSuccessfulCycles)
        {
            StartOrbitRewardOrComplete();
        }
        else
        {
            runtime.BeginRecallWindow(activeDefinition.RecallWindowDuration);
            performanceTracker.BeginCycle(runtime.CompletedCycles + 1);
            UpdateWindowUI();
        }
    }

    private void TickRecallWindow(PlayerInputReader input)
    {
        UpdateWindowUI();

        if (activeProjectile == null)
        {
            FailSequence("Recall window without active projectile.");
            return;
        }

        HandleDashInput(
            input,
            activeDefinition.AllowDashDuringRecall,
            OnRecallDashSuccess,
            "Bad dash timing during recall window.");

        if (!runtime.IsRunning)
            return;

        if (runtime.IsWindowExpired())
        {
            ForceWindowUIToEnd(activeDefinition.RecallRule, "Recall", false);
            uiPresenter?.FlashJudgement(default);
            activeProjectile.EnterDriftLost();
            FailSequence("Recall timing expired.");
            return;
        }

        if (!input.ConsumePrimaryFireRequest())
            return;

        TimingJudgement judgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.RecallRule);

        uiPresenter?.FlashJudgement(judgement);

        if (!IsSuccess(judgement))
        {
            activeProjectile.EnterDriftLost();
            FailSequence("Recall input outside recall rule.");
            return;
        }

        ForceWindowUIToEnd(activeDefinition.RecallRule, "Recall", false);
        pendingTransition.SetBeginReturn(activeDefinition.UiPhaseTransitionHoldDuration);
    }

    private void TickReturningToReflectZone(PlayerInputReader input)
    {
        UpdateWindowUI();

        if (activeProjectile == null)
        {
            FailSequence("Returning phase without projectile.");
            return;
        }

        HandleDashInput(
            input,
            activeDefinition.AllowDashDuringRecall,
            OnRecallDashSuccess,
            "Bad dash timing while returning to reflect zone.");

        if (!runtime.IsRunning)
            return;
    }

    private void TickReflectWindow(PlayerInputReader input)
    {
        UpdateWindowUI();

        if (activeProjectile == null)
        {
            FailSequence("Reflect window without projectile.");
            return;
        }

        HandleDashInput(
            input,
            activeDefinition.AllowDashDuringReflect,
            OnReflectDashSuccess,
            "Bad dash timing during reflect window.");

        if (!runtime.IsRunning)
            return;

        if (runtime.IsWindowExpired())
        {
            ForceWindowUIToEnd(activeDefinition.ReflectRule, "Reflect", false);
            uiPresenter?.FlashJudgement(default);
            activeProjectile.EnterDriftLost();
            FailSequence("Reflect timing expired.");
        }
    }

    private void HandleDashInput(PlayerInputReader input, bool dashAllowed, System.Action onSuccess, string failReason)
    {
        if (!dashAllowed)
            return;

        if (!input.ConsumeDashPressed())
            return;

        TimingJudgement dashJudgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.DashRule);

        uiPresenter?.FlashJudgement(dashJudgement);

        if (IsSuccess(dashJudgement))
        {
            onSuccess?.Invoke();
        }
        else if (activeDefinition.FailOnBadDash)
        {
            if (activeProjectile != null)
                activeProjectile.EnterDriftLost();

            FailSequence(failReason);
        }
    }

    private void OnRecallDashSuccess()
    {
        runtime.RegisterRecallDashSuccess();

        if (activeProjectile == null || activeWeaponData == null)
            return;

        activeProjectile.ApplyReturnDashBoost(
            activeWeaponData.DashReturnSpeedMultiplierBonus,
            activeWeaponData.DashReturnSteeringBonus);
    }

    private void OnReflectDashSuccess()
    {
        runtime.RegisterReflectDashSuccess();

        if (activeProjectile == null || activeWeaponData == null)
            return;

        activeProjectile.ApplyNextReflectDashBoost(
            activeWeaponData.DashReflectSpeedMultiplierBonus);
    }

    private void StartOrbitRewardOrComplete()
    {
        if (activeDefinition == null || activeProjectile == null || activeActorAdapter == null || !activeDefinition.UseOrbitReward)
        {
            CompleteSequence(destroyProjectile: true);
            return;
        }

        SequenceRewardContextBase rewardContext =
            rewardEvaluator.BuildContext(
                performanceTracker.Performance,
                runtime.CompletedCycles,
                runtime.CompletedCycles);

        SequenceRewardResolution resolution =
            rewardEvaluator.Evaluate(activeDefinition.RewardPolicy, rewardContext);

        if (!resolution.shouldApply)
        {
            if (debugLogs)
            {
                Debug.Log(
                    $"[BoomerangSequenceController] Orbit reward skipped by policy. uniqueEnemies={rewardContext.uniqueTargetCount} totalHits={rewardContext.hitCount}",
                    this);
            }

            CompleteSequence(destroyProjectile: true);
            return;
        }

        runtime.BeginOrbitReward();
        orbitRewardActive = true;

        uiPresenter?.Hide();

        playerReferences?.Combat?.CancelAllAttacks();
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        if (debugLogs)
        {
            Debug.Log(
                $"[BoomerangSequenceController] Orbit reward granted. uniqueEnemies={rewardContext.uniqueTargetCount} totalHits={rewardContext.hitCount} finalDuration={resolution.duration:F2}",
                this);
        }

        BoomerangOrbitRewardApplySO orbitRewardApply =
            activeDefinition.CompletionReward as BoomerangOrbitRewardApplySO;

        if (orbitRewardApply != null)
        {
            orbitRewardApply.ApplyToBoomerang(
                rewardContext,
                resolution,
                activeActorAdapter,
                activeDefinition.OrbitDuration);
        }
        else
        {
            activeActorAdapter.BeginReward(
                resolution.duration > 0f ? resolution.duration : activeDefinition.OrbitDuration,
                0);
        }
    }

    private void CompleteSequence(bool destroyProjectile)
    {
        BoomerangProjectile2D projectileToDestroy = activeProjectile;

        runtime.Complete();

        uiPresenter?.Hide();
        playerReferences?.Combat?.CancelAllAttacks();
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        UnbindProjectileEvents();

        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;
        orbitRewardActive = false;

        pendingTransition.Clear();

        if (destroyProjectile && projectileToDestroy != null)
            Destroy(projectileToDestroy.gameObject);

        if (debugLogs)
            Debug.Log("[BoomerangSequenceController] Sequence completed.", this);
    }

    private void FailSequence(string reason)
    {
        BoomerangProjectile2D projectileToDestroy = activeProjectile;
        BoomerangSequenceActorAdapter actorAdapterToCleanup = activeActorAdapter;

        Debug.LogWarning(
            $"[BoomerangSequenceController] FAIL reason='{reason}' phase={runtime.Phase} cycles={runtime.CompletedCycles} projectile={(projectileToDestroy != null ? projectileToDestroy.name : "null")}",
            this);

        bool clearOverride = activeDefinition == null || activeDefinition.ClearWeaponOverrideOnFail;
        bool destroyProjectileOnFail = activeDefinition == null || activeDefinition.DestroyProjectileOnFail;
        float destroyDelay = activeDefinition != null ? activeDefinition.DestroyProjectileOnFailDelay : 0f;

        runtime.Fail();
        pendingTransition.Clear();

        UnbindProjectileEvents();
        uiPresenter?.Hide();

        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;
        activeActorAdapter = null;
        orbitRewardActive = false;

        if (clearOverride)
            playerReferences?.WeaponOverride?.ClearActiveOverride();

        if (destroyProjectileOnFail && actorAdapterToCleanup != null)
        {
            actorAdapterToCleanup.FailAndCleanup(destroyDelay);
        }
        else if (destroyProjectileOnFail && projectileToDestroy != null)
        {
            if (destroyDelay <= 0f)
                Destroy(projectileToDestroy.gameObject);
            else
                Destroy(projectileToDestroy.gameObject, destroyDelay);
        }
    }

    private void UpdateWindowUI()
    {
        if (activeDefinition == null || rewardEvaluator == null || uiPresenter == null)
            return;

        SequenceRewardContextBase previewContext =
            rewardEvaluator.BuildContext(
                performanceTracker.Performance,
                runtime.CompletedCycles,
                runtime.CompletedCycles + 1);

        SequenceRewardResolution previewResolution =
            rewardEvaluator.Evaluate(activeDefinition.RewardPolicy, previewContext);

        SequenceRewardPreviewInfo previewInfo =
            rewardEvaluator.BuildPreview(activeDefinition.RewardPolicy, previewContext, previewResolution);

        SequencePerformanceUISnapshot snapshot =
            performanceTracker.Performance.BuildGenericUISnapshot(
                currentProgress: runtime.CompletedCycles,
                requiredProgress: activeDefinition.RequiredSuccessfulCycles,
                rewardEligible: previewResolution.shouldApply);

        snapshot.rewardStateText = previewInfo.stateText;
        snapshot.rewardFormulaText = previewInfo.formulaText;
        snapshot.rewardResultText = previewInfo.resultText;

        uiPresenter.Update(runtime, activeDefinition, snapshot);
    }

    

    private void ForceWindowUIToEnd(TimedSequenceActionRule rule, string phaseLabel, bool useNeutralBar)
    {
        if (activeDefinition == null || uiPresenter == null)
            return;

        uiPresenter.ForceWindowToEnd(
            runtime,
            activeDefinition,
            rule,
            phaseLabel,
            useNeutralBar);
    }

    

    private void BindProjectileEvents(BoomerangProjectile2D projectile)
    {
        if (projectile == null)
            return;

        projectile.onBecameReflectable += OnProjectileBecameReflectable;
        projectile.onReturnedToOwner += OnProjectileReturnedToOwner;
        projectile.onFinished += OnProjectileFinished;
        projectile.onLost += OnProjectileLost;
        projectile.onOrbitRewardFinished += OnOrbitRewardFinished;
    }

    private void UnbindProjectileEvents()
    {
        if (activeProjectile == null)
            return;

        activeProjectile.onBecameReflectable -= OnProjectileBecameReflectable;
        activeProjectile.onReturnedToOwner -= OnProjectileReturnedToOwner;
        activeProjectile.onFinished -= OnProjectileFinished;
        activeProjectile.onLost -= OnProjectileLost;
        activeProjectile.onOrbitRewardFinished -= OnOrbitRewardFinished;
    }

    private void OnProjectileBecameReflectable(BoomerangProjectile2D projectile)
    {
        if (!runtime.IsRunning || projectile != activeProjectile || activeDefinition == null)
            return;

        if (runtime.Phase != BoomerangSequencePhase.ReturningToReflectZone)
            return;

        ForceWindowUIToEnd(null, "Return", true);
        runtime.BeginReflectWindow(activeDefinition.ReflectWindowDuration);
        UpdateWindowUI();
    }

    private void OnProjectileReturnedToOwner(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile)
            return;

        if (runtime.Phase == BoomerangSequencePhase.OutboundRecallWindow ||
            runtime.Phase == BoomerangSequencePhase.ReturningToReflectZone ||
            runtime.Phase == BoomerangSequencePhase.ReflectWindow)
        {
            FailSequence("Projectile returned to owner unexpectedly during active sequence.");
        }
    }

    private void OnProjectileFinished(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile)
            return;

        if (runtime.IsInOrbitReward)
        {
            CompleteSequence(destroyProjectile: false);
            return;
        }

        if (runtime.IsRunning)
            FailSequence("Projectile finished while sequence was active.");
    }

    private void OnProjectileLost(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile)
            return;

        if (runtime.IsRunning)
            FailSequence("Projectile was lost during sequence.");
    }

    private void OnOrbitRewardFinished(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile)
            return;

        CompleteSequence(destroyProjectile: false);
        Destroy(projectile.gameObject);
    }

    private static TimingJudgement EvaluateTiming(float normalizedTime, TimedSequenceActionRule rule)
    {
        if (rule == null)
            return default;

        float center = 0.5f;
        float distance = Mathf.Abs(normalizedTime - center);

        if (rule.AllowPerfect && distance <= rule.PerfectHalfWindowNormalized)
            return TimingJudgement.Perfect;

        if (distance <= rule.GoodHalfWindowNormalized)
            return TimingJudgement.Good;

        return default;
    }

    private static bool IsSuccess(TimingJudgement judgement)
    {
        return judgement == TimingJudgement.Good || judgement == TimingJudgement.Perfect;
    }

    private static BoomerangDamageActionType ResolveDamageActionType(BoomerangFlightState state)
    {
        return state switch
        {
            BoomerangFlightState.Outbound => BoomerangDamageActionType.Outbound,
            BoomerangFlightState.ReturningCurved => BoomerangDamageActionType.Returning,
            BoomerangFlightState.ReflectableReturning => BoomerangDamageActionType.ReflectHold,
            BoomerangFlightState.ReflectedOutbound => BoomerangDamageActionType.ReflectedOutbound,
            BoomerangFlightState.OrbitingExpanding => BoomerangDamageActionType.OrbitReward,
            _ => BoomerangDamageActionType.Unknown
        };
    }

}