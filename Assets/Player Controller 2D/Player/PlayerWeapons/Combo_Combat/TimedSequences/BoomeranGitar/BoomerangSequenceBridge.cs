using UnityEngine;

public class BoomerangSequenceBridge : MonoBehaviour
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

        sequenceUI?.ShowBoomerang(activeDefinition, playerReferences);
        UpdateWindowUI();

        if (debugLogs)
            Debug.Log("[BoomerangSequenceBridge] Sequence started.", this);

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

        sequenceUI?.FlashJudgement(judgement);

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
                $"[BoomerangSequenceBridge] Damage registered action={actionType} totalHits={performance.TotalHitEvents} totalUnique={performance.TotalUniqueEnemiesDamaged} cycle={performance.CurrentCycleNumber} cycleHits={performance.CurrentCycleHitEvents} cycleUnique={performance.CurrentCycleUniqueEnemiesDamaged}",
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

        sequenceUI?.Hide();

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
            sequenceUI?.FlashJudgement(default);
            activeProjectile.EnterDriftLost();
            FailSequence("Recall timing expired.");
            return;
        }

        if (!input.ConsumePrimaryFireRequest())
            return;

        TimingJudgement judgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.RecallRule);

        sequenceUI?.FlashJudgement(judgement);

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
            sequenceUI?.FlashJudgement(default);
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

        sequenceUI?.FlashJudgement(dashJudgement);

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

        SequenceRewardPolicySOBase rewardPolicy = activeDefinition.RewardPolicy;

        SequenceRewardContextBase rewardContext =
            performanceTracker.Performance.BuildRewardContextBase(
                sequenceCompleted: true,
                completedSteps: runtime.CompletedCycles,
                attemptedSteps: runtime.CompletedCycles);

        SequenceRewardResolution resolution =
            rewardPolicy != null
                ? rewardPolicy.Evaluate(rewardContext, null)
                : new SequenceRewardResolution
                {
                    shouldApply = true,
                    duration = activeDefinition.OrbitDuration,
                    ammo = 0,
                    magnitude = 0f
                };

        if (!resolution.shouldApply)
        {
            if (debugLogs)
            {
                Debug.Log(
                    $"[BoomerangSequenceBridge] Orbit reward skipped by policy. uniqueEnemies={rewardContext.uniqueTargetCount} totalHits={rewardContext.hitCount}",
                    this);
            }

            CompleteSequence(destroyProjectile: true);
            return;
        }

        float orbitDuration = resolution.duration > 0f
            ? resolution.duration
            : activeDefinition.OrbitDuration;

        runtime.BeginOrbitReward();
        orbitRewardActive = true;

        sequenceUI?.Hide();

        playerReferences?.Combat?.CancelAllAttacks();
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        if (debugLogs)
        {
            Debug.Log(
                $"[BoomerangSequenceBridge] Orbit reward granted. uniqueEnemies={rewardContext.uniqueTargetCount} totalHits={rewardContext.hitCount} finalDuration={orbitDuration}",
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
                activeDefinition.OrbitDuration,
                activeDefinition.OrbitTurns);
        }
        else
        {
            activeActorAdapter.BeginReward(
                orbitDuration,
                activeDefinition.OrbitTurns);
        }
    }

    private void CompleteSequence(bool destroyProjectile)
    {
        BoomerangProjectile2D projectileToDestroy = activeProjectile;

        runtime.Complete();

        sequenceUI?.Hide();
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
            Debug.Log("[BoomerangSequenceBridge] Sequence completed.", this);
    }

    private void FailSequence(string reason)
    {
        BoomerangProjectile2D projectileToDestroy = activeProjectile;

        Debug.LogWarning(
            $"[BoomerangSequenceBridge] FAIL reason='{reason}' phase={runtime.Phase} cycles={runtime.CompletedCycles} projectile={(projectileToDestroy != null ? projectileToDestroy.name : "null")}",
            this);

        bool clearOverride = activeDefinition == null || activeDefinition.ClearWeaponOverrideOnFail;
        bool destroyProjectileOnFail = activeDefinition == null || activeDefinition.DestroyProjectileOnFail;
        float destroyDelay = activeDefinition != null ? activeDefinition.DestroyProjectileOnFailDelay : 0f;

        runtime.Fail();
        pendingTransition.Clear();

        UnbindProjectileEvents();
        sequenceUI?.Hide();

        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;
        activeActorAdapter = null;
        orbitRewardActive = false;
        activeActorAdapter = null;

        if (clearOverride)
            playerReferences?.WeaponOverride?.ClearActiveOverride();

        if (destroyProjectileOnFail && activeActorAdapter != null)
        {
            activeActorAdapter.FailAndCleanup(destroyDelay);
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
        if (sequenceUI == null || activeDefinition == null)
            return;

        bool useNeutralBar =
            runtime.Phase == BoomerangSequencePhase.ReturningToReflectZone;

        sequenceUI.SetBoomerangWindowProgress(
            runtime.GetWindowNormalizedTime(),
            runtime.CompletedCycles,
            activeDefinition.RequiredSuccessfulCycles,
            GetActiveRuleForCurrentPhase(),
            GetCurrentPhaseLabel(),
            useNeutralBar);

        SequenceRewardPreviewInfo previewInfo = BuildRewardPreviewInfo();

        SequencePerformanceUISnapshot snapshot =
            performanceTracker.Performance.BuildGenericUISnapshot(
                currentProgress: runtime.CompletedCycles,
                requiredProgress: activeDefinition.RequiredSuccessfulCycles,
                rewardEligible: previewInfo.stateText == "READY");

        snapshot.rewardStateText = previewInfo.stateText;
        snapshot.rewardFormulaText = previewInfo.formulaText;
        snapshot.rewardResultText = previewInfo.resultText;

        sequenceUI.SetPerformanceSnapshot(snapshot);
    }

    private SequenceRewardPreviewInfo BuildRewardPreviewInfo()
    {
        if (activeDefinition == null)
            return SequenceRewardPreviewInfo.Empty;

        SequenceRewardPolicySOBase rewardPolicy = activeDefinition.RewardPolicy;
        if (rewardPolicy == null)
            return SequenceRewardPreviewInfo.Empty;

        SequenceRewardContextBase previewContext =
            performanceTracker.Performance.BuildRewardContextBase(
                sequenceCompleted: true,
                completedSteps: runtime.CompletedCycles,
                attemptedSteps: runtime.CompletedCycles + 1);

        SequenceRewardResolution previewResolution = rewardPolicy.Evaluate(previewContext, null);
        return rewardPolicy.BuildPreview(previewContext, null, previewResolution);
    }

    private void ForceWindowUIToEnd(TimedSequenceActionRule rule, string phaseLabel, bool useNeutralBar)
    {
        if (sequenceUI == null || activeDefinition == null)
            return;

        sequenceUI.SetBoomerangWindowProgress(
            1f,
            runtime.CompletedCycles,
            activeDefinition.RequiredSuccessfulCycles,
            rule,
            phaseLabel,
            useNeutralBar);
    }

    private TimedSequenceActionRule GetActiveBarRule()
    {
        return runtime.Phase switch
        {
            BoomerangSequencePhase.OutboundRecallWindow => activeDefinition.RecallRule,
            BoomerangSequencePhase.ReturningToReflectZone => null,
            BoomerangSequencePhase.ReflectWindow => activeDefinition.ReflectRule,
            _ => null
        };
    }

    private string GetPhaseLabel()
    {
        return runtime.Phase switch
        {
            BoomerangSequencePhase.OutboundRecallWindow => "Recall",
            BoomerangSequencePhase.ReturningToReflectZone => "Return",
            BoomerangSequencePhase.ReflectWindow => "Reflect",
            BoomerangSequencePhase.OrbitReward => "Orbit",
            BoomerangSequencePhase.Completed => "Complete",
            BoomerangSequencePhase.Failed => "Fail",
            _ => "Boomerang"
        };
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

    private bool EvaluateRewardEligibilityPreview()
    {
        if (activeDefinition == null)
            return false;

        SequenceRewardPolicySOBase rewardPolicy = activeDefinition.RewardPolicy;
        if (rewardPolicy == null)
            return false;

        SequenceRewardContextBase previewContext =
            performanceTracker.Performance.BuildRewardContextBase(
                sequenceCompleted: true,
                completedSteps: runtime.CompletedCycles,
                attemptedSteps: runtime.CompletedCycles + 1);

        SequenceRewardResolution preview = rewardPolicy.Evaluate(previewContext, null);
        return preview.shouldApply;
    }

    private TimedSequenceActionRule GetActiveRuleForCurrentPhase()
    {
        if (activeDefinition == null)
            return null;

        return runtime.Phase switch
        {
            BoomerangSequencePhase.OutboundRecallWindow => activeDefinition.RecallRule,
            BoomerangSequencePhase.ReturningToReflectZone => activeDefinition.ReflectRule,
            BoomerangSequencePhase.ReflectWindow => activeDefinition.ReflectRule,
            _ => activeDefinition.RecallRule
        };
    }

    private string GetCurrentPhaseLabel()
    {
        return runtime.Phase switch
        {
            BoomerangSequencePhase.OutboundRecallWindow => "Recall",
            BoomerangSequencePhase.ReturningToReflectZone => "Return",
            BoomerangSequencePhase.ReflectWindow => "Reflect",
            BoomerangSequencePhase.OrbitReward => "Orbit",
            _ => "Sequence"
        };
    }
}