using UnityEngine;

public class BoomerangLoopController : MonoBehaviour, IBoomerangSequenceBridge
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController sequenceUI;
    [SerializeField] private WeaponBehaviour boomerangWeapon;
    [SerializeField] private Transform catchAnchor;

    [SerializeField] private RhythmCombatController rhythmCombat;
    [SerializeField] private GlobalRhythmContextResolver globalRhythmContext;
    [SerializeField] private RhythmClock rhythmClock;

    [SerializeField] private Color globalHoldOverlayColor = new Color(0.35f, 1f, 0.7f, 0.95f);
    [SerializeField, Min(0f)] private float globalHoldReleaseBeatOffset = 1f;
    [SerializeField] private Color globalDecisionOverlayColor = new Color(1f, 0.9f, 0.3f, 0.95f);


    [SerializeField] private float decisionReleaseInputLockSeconds = 0.04f;


    [Header("Decision Reflect Visual")]
    [SerializeField] private MeleeAnimatedWeaponDataSO decisionReflectVisualData;
    [SerializeField] private Transform decisionReflectSpawnPoint;
    [SerializeField] private bool playDecisionReflectVisual = true;


    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;
    [SerializeField] private bool debugRewardLogs = false;


    [Header("Runtime")]
    [SerializeField] private BoomerangLoopSequenceRuntime runtime = new();
    [SerializeField] private BoomerangSequencePerformanceTracker performanceTracker = new();

    private BoomerangProjectile2D activeProjectile;
    private WeaponBehaviour activeWeapon;
    private BoomerangLoopSequenceDefinitionSO activeDefinition;

    private BoomerangSequenceRewardEvaluator rewardEvaluator;

    private int relaunchSuccessCount;
    private int reflectSuccessCount;
    private int relaunchPerfectCount;
    private int reflectPerfectCount;
    private int shotRedirectSuccessCount;
    private int shotRedirectPerfectCount;
    private float weightedScore;


    private GlobalRhythmPromptType lastAppliedGlobalPrompt = GlobalRhythmPromptType.None;
    private bool globalBoomerangContextApplied;
    private float catchDecisionStartTime;
    private bool sawBoomerangHeldInDecision;
    private int recallAttemptsRemaining;
    private int recallAttemptsTotal;

    private bool pendingRecallPostRedirect;
    private bool waitingForRecallBeat;
    private bool waitingForDecisionBeat;

    private bool recallIntentBuffered;
    private bool releaseIntentBuffered;
    private bool reflectIntentBuffered;

    private float recallIntentBufferedTime;
    private float releaseIntentBufferedTime;
    private float reflectIntentBufferedTime;
    private bool allowHeldRecallAfterDecision;
    private float heldRecallAfterDecisionGraceEndTime;
    [SerializeField] private float heldRecallAfterDecisionGraceSeconds = 0.25f;


    private bool pendingDecisionRelease;
    private float pendingDecisionReleaseTime;
    private TimingJudgement pendingDecisionReleaseJudgement;

    private string lastDecisionInputText = "-";
    private string lastDecisionWindowStateText = "-";

    private bool pendingRecallShotRedirect;
    private float pendingRecallShotRedirectStartTime;
    private TimingJudgement pendingRecallShotRedirectJudgement;

    private bool isPostRedirectRecall;


    private string failReasonText = string.Empty;
    private bool suppressProjectileLostFail;
    public bool IsSequenceActive => runtime.IsRunning;
    public bool HasActiveProjectile => activeProjectile != null;
    public bool IsInOrbitReward => runtime.IsInOrbitReward;
    public Transform CatchAnchor => catchAnchor;
    public BoomerangLoopController BoomerangLoop => this;

    private void Awake()
    {
        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();

        if (sequenceUI == null)
            sequenceUI = GetComponentInChildren<TimedSequenceUIController>(true);

        if (rhythmCombat == null)
            rhythmCombat = FindFirstObjectByType<RhythmCombatController>();

        if (globalRhythmContext == null)
            globalRhythmContext = FindFirstObjectByType<GlobalRhythmContextResolver>();

        if (rhythmClock == null)
            rhythmClock = FindFirstObjectByType<RhythmClock>();

        rewardEvaluator = new BoomerangSequenceRewardEvaluator();
    }

    public void SetAim(Vector2 dir)
    {
        if (boomerangWeapon != null)
            boomerangWeapon.SetAim(dir);
    }

    public void TickLoop(PlayerInputReader input)
    {

        if (input == null)
            return;

        CaptureInputIntents(input);

        if (!runtime.IsRunning)
        {
            TryStartFromIdle(input);
            return;
        }

        UpdateUI();
        UpdateGlobalRhythmContext();

        switch (runtime.Phase)
        {
            case BoomerangLoopSequencePhase.OutboundRecallWindow:
                TickRecallWindow(input);
                break;

            case BoomerangLoopSequencePhase.ShotRedirectedOutbound:
                TickShotRedirectedOutbound();
                break;

            case BoomerangLoopSequencePhase.ReturningHold:
                TickReturningHold(input);
                break;

            case BoomerangLoopSequencePhase.CatchDecisionWindow:
                TickCatchDecisionWindow(input);
                break;

            case BoomerangLoopSequencePhase.Recovery:
                TickRecovery();
                break;

            case BoomerangLoopSequencePhase.RecallPendingBeat:
                TickRecallPendingBeat();
                break;

            case BoomerangLoopSequencePhase.DecisionPendingBeat:
                TickDecisionPendingBeat(input);
                break;

            case BoomerangLoopSequencePhase.FailCooldown:
                TickFailCooldown();
                break;
        }
    }

    private void CaptureInputIntents(PlayerInputReader input)
    {
        if (input == null)
            return;

        if (input.ConsumeBoomerangPressed())
        {
            recallIntentBuffered = true;
            recallIntentBufferedTime = Time.time;
        }

        if (input.ConsumeBoomerangReleased())
        {
            releaseIntentBuffered = true;
            releaseIntentBufferedTime = Time.time;
        }

        if (input.ConsumeSecondaryFireRequest())
        {
            reflectIntentBuffered = true;
            reflectIntentBufferedTime = Time.time;
        }
    }

    private void ClearBufferedIntents()
    {
        recallIntentBuffered = false;
        releaseIntentBuffered = false;
        reflectIntentBuffered = false;

        recallIntentBufferedTime = 0f;
        releaseIntentBufferedTime = 0f;
        reflectIntentBufferedTime = 0f;
    }

    public bool BeginLoop(BoomerangProjectile2D projectile, WeaponBehaviour weapon, BoomerangLoopWeaponDataSO weaponData)
    {
        if (projectile == null || weapon == null || weaponData == null || weaponData.loopSequenceDefinition == null)
            return false;

        if (runtime.IsRunning || activeProjectile != null)
            return false;

        activeProjectile = projectile;
        activeWeapon = weapon;
        activeDefinition = weaponData.loopSequenceDefinition;

        ResetLoopCounters();
        ResetDecisionState();
        ResetFailState();

        performanceTracker.ResetSequence();
        performanceTracker.BeginCycle(1);

        BindProjectileEvents();

        runtime.Reset();

        sequenceUI?.ShowBoomerang(activeDefinition, playerReferences);
        BeginRecallPhase(false);

        ApplyBoomerangWeaponContext();
        UpdateGlobalRhythmContext();

        allowHeldRecallAfterDecision = false;
        heldRecallAfterDecisionGraceEndTime = 0f;

        if (debugLogs)
            Debug.Log("[BoomerangLoopController] Loop started.", this);

        return true;
    }

    // Reservado para futuras variantes con reflect melee separado.
    // El loop unificado actual resuelve L2 dentro de CatchDecisionWindow.
    public bool TryResolveMeleeReflect(BoomerangProjectile2D projectile, DeflectInfo info)
    {
        return false;
    }
   

    public void RegisterBoomerangDamage(BoomerangProjectile2D projectile, Collider2D other, BoomerangFlightState flightState)
    {
        if (projectile == null || other == null || projectile != activeProjectile)
            return;

        BoomerangDamageActionType actionType = flightState switch
        {
            BoomerangFlightState.Outbound => BoomerangDamageActionType.Outbound,
            BoomerangFlightState.ReturningCurved => BoomerangDamageActionType.Returning,
            BoomerangFlightState.ReflectableReturning => BoomerangDamageActionType.ReflectHold,
            BoomerangFlightState.ReflectedOutbound => BoomerangDamageActionType.ReflectedOutbound,
            BoomerangFlightState.OrbitingExpanding => BoomerangDamageActionType.OrbitReward,
            _ => BoomerangDamageActionType.Unknown
        };

        performanceTracker.RegisterDamage(other, actionType);
    }



    private void TryStartFromIdle(PlayerInputReader input)
    {
        if (boomerangWeapon == null)
        {
            Debug.LogWarning("[BoomerangLoopController] Missing boomerangWeapon.", this);
            return;
        }

        if (!recallIntentBuffered)
            return;

        recallIntentBuffered = false;
        recallIntentBufferedTime = 0f;

        BoomerangLoopWeaponDataSO loopData = boomerangWeapon.WeaponData as BoomerangLoopWeaponDataSO;
        BoomerangLoopSequenceDefinitionSO definition = loopData != null ? loopData.loopSequenceDefinition : null;

        bool requireRhythmGate = definition != null && definition.RequireRhythmOnInitialLaunch;

        if (requireRhythmGate && rhythmCombat != null)
        {
            RhythmInputResult result = rhythmCombat.RegisterAttack(CombatAction.Special);

            bool launchAccepted =
                result.quality == RhythmHitQuality.Good ||
                result.quality == RhythmHitQuality.Perfect;

            if (!launchAccepted)
            {
                if (debugLogs)
                {
                    Debug.Log(
                        $"[BoomerangLoopController] Initial launch blocked by rhythm gate. quality={result.quality} dist={result.distanceToBeat:F3}s",
                        this);
                }
                ShowGlobalJudgementInfo("LAUNCH", default);
                return;
            }

            ShowGlobalJudgementInfo("LAUNCH", result.quality switch
            {
                RhythmHitQuality.Perfect => TimingJudgement.Perfect,
                RhythmHitQuality.Good => TimingJudgement.Good,
                _ => default
            });

            if (debugLogs)
            {
                Debug.Log(
                    $"[BoomerangLoopController] Initial launch accepted by rhythm gate. quality={result.quality} dist={result.distanceToBeat:F3}s",
                    this);
            }
        }

        Debug.Log("[BoomerangLoopController] Boomerang pressed consumed. Trying to fire.", this);

        bool fired = boomerangWeapon.TryFire();
        Debug.Log($"[BoomerangLoopController] TryFire result = {fired}", this);
    }

    private void TickRecallWindow(PlayerInputReader input)
    {
        if (activeProjectile == null || activeDefinition == null)
        {
            ForceStop();
            return;
        }

        if (pendingRecallShotRedirect)
        {
            if (Time.time >= pendingRecallShotRedirectStartTime + activeDefinition.RecallShotRedirectWindowDuration)
            {
                pendingRecallShotRedirect = false;
                pendingRecallShotRedirectStartTime = 0f;
                pendingRecallShotRedirectJudgement = default;

                lastDecisionInputText = "R2 WINDOW EXPIRED";
                lastDecisionWindowStateText = "NONE";

                if (debugLogs)
                    Debug.Log("[BoomerangLoopController] Recall shot redirect window expired.", this);

                UpdateUI();
            }
        }

        if (runtime.IsWindowExpired())
        {
            if (AdvanceRecallAttemptWindow())
                return;

            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Recall window expired -> fail.", this);

            suppressProjectileLostFail = true;
            activeProjectile.EnterDriftLost();
            EnterFailCooldown("TIME OUT");
            suppressProjectileLostFail = false;
            return;
        }


        // Si ya elegiste R2, L1 queda bloqueado hasta que expire o se resuelva el redirect.
        if (pendingRecallShotRedirect)
            return;

        // 2) L1: recall normal.
        bool recallIntent = recallIntentBuffered;

        if (!recallIntent && allowHeldRecallAfterDecision && input != null && input.BoomerangHeld)
            recallIntent = true;

        if (!recallIntent)
            return;

        recallIntentBuffered = false;
        recallIntentBufferedTime = 0f;

        TimingJudgement recallJudgement = EvaluateRecallJudgement();

        sequenceUI?.FlashJudgement(recallJudgement);

        ShowGlobalJudgementInfo("RECALL", recallJudgement);

        if (!IsSuccess(recallJudgement))
        {
            if (debugLogs)
                Debug.Log($"[BoomerangLoopController] Recall failed. judgement={recallJudgement}", this);

            suppressProjectileLostFail = true;
            activeProjectile.EnterDriftLost();
            EnterFailCooldown("MISS");
            suppressProjectileLostFail = false;
            return;
        }

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Recall success. judgement={recallJudgement}", this);

        isPostRedirectRecall = false;

        float holdDuration = activeDefinition.ResolveReturnHoldDuration(rhythmClock);

        activeProjectile.StartCurvedReturn(holdDuration, 1f);
        runtime.BeginReturningHold(holdDuration);

        UpdateUI();
    }

    private void TickShotRedirectedOutbound()
    {
        if (activeProjectile == null || activeDefinition == null)
        {
            ForceStop();
            return;
        }
        if (runtime.IsWindowExpired())
        {
            lastDecisionInputText = "WAIT L1";
            lastDecisionWindowStateText = "NONE";

            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Redirect finished -> entering Recall 2.", this);

            BeginRecallPhase(true);
        }
    }

    public bool CanArmRecallShotRedirect()
    {
        return runtime.IsRunning &&
               runtime.Phase == BoomerangLoopSequencePhase.OutboundRecallWindow &&
               activeProjectile != null &&
               activeDefinition != null &&
               !pendingRecallShotRedirect &&
               !isPostRedirectRecall;
    }

    public bool TryArmRecallShotRedirectFromPrimaryFire()
    {
        if (!CanArmRecallShotRedirect())
            return false;

        TimingJudgement shotJudgement = EvaluateRecallJudgement();

        sequenceUI?.FlashJudgement(shotJudgement);

        if (shotJudgement != TimingJudgement.Good && shotJudgement != TimingJudgement.Perfect)
        {
            if (debugLogs)
                Debug.Log($"[BoomerangLoopController] Recall shot redirect ignored. judgement={shotJudgement}", this);

            return false;
        }

        pendingRecallShotRedirect = true;
        pendingRecallShotRedirectStartTime = Time.time;
        pendingRecallShotRedirectJudgement = shotJudgement;

        lastDecisionInputText = "R2 SHOT (WAIT HIT)";
        lastDecisionWindowStateText = shotJudgement == TimingJudgement.Perfect ? "PERFECT" : "GOOD";

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Recall shot redirect armed. judgement={shotJudgement}", this);

        UpdateUI();
        return true;
    }

    private void OnProjectileLost(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile)
            return;

        if (suppressProjectileLostFail)
            return;

        if (debugLogs)
            Debug.Log("[BoomerangLoopController] Projectile lost -> fail.", this);

        EnterFailCooldown("LOST");
    }

    private void TickReturningHold(PlayerInputReader input)
    {
        if (activeProjectile == null || activeDefinition == null)
        {
            ForceStop();
            return;
        }

        if (!input.BoomerangHeld)
        {
            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Early release during hold -> recovery.", this);

            runtime.BeginRecovery(activeDefinition.RecoveryCooldownOnEarlyRelease);
            UpdateUI();
        }

        if (TryActivateOrbitReward(BoomerangLoopRewardTriggerInput.OnHoldL1))
            return;
    }


    private void TickCatchDecisionWindow(PlayerInputReader input)
    {
        if (activeDefinition == null || activeProjectile == null)
        {
            ForceStop();
            return;
        }

        // 1) RELEASE L1 => Good o Perfect => relaunch
        if (input.BoomerangHeld)
            sawBoomerangHeldInDecision = true;

        bool rawReleasedThisFrame = releaseIntentBuffered;

        bool releaseAllowed =
            sawBoomerangHeldInDecision &&
            rawReleasedThisFrame &&
            Time.time >= catchDecisionStartTime + Mathf.Max(0f, decisionReleaseInputLockSeconds);

        if (rawReleasedThisFrame && debugLogs)
        {
            Debug.Log(
                $"[BoomerangLoopController] ConsumeBoomerangReleased() = TRUE | " +
                $"releaseAllowed={releaseAllowed} " +
                $"sawHeldInDecision={sawBoomerangHeldInDecision} " +
                $"timeSinceDecisionStart={(Time.time - catchDecisionStartTime):F3}",
                this);
        }

        if (rawReleasedThisFrame && releaseAllowed)
        {
            releaseIntentBuffered = false;
            releaseIntentBufferedTime = 0f;

            TimingJudgement judgement = EvaluateCatchDecisionJudgement();

            lastDecisionInputText = "L1 RELEASE (QUEUED)";

            lastDecisionWindowStateText = judgement switch
            {
                TimingJudgement.Perfect => "PERFECT",
                TimingJudgement.Good => "GOOD",
                _ => "NONE"
            };

            sequenceUI?.FlashJudgement(judgement);

            ShowGlobalJudgementInfo("L1 RELEASE", judgement);

            if (debugLogs)
                Debug.Log($"[BoomerangLoopController] Catch decision L1 release. judgement={judgement}", this);

            if (judgement == TimingJudgement.Good || judgement == TimingJudgement.Perfect)
            {
                pendingDecisionRelease = true;
                pendingDecisionReleaseTime = Time.time;
                pendingDecisionReleaseJudgement = judgement;

                if (debugLogs)
                    Debug.Log("[BoomerangLoopController] L1 release queued, waiting short grace for possible L2 override.", this);

                return;
            }

            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Bad L1 release ignored. Decision window stays open.", this);

            return;
        }

        // 2) L2 => solo Perfect => reflect
        if (reflectIntentBuffered)
        {
            reflectIntentBuffered = false;
            reflectIntentBufferedTime = 0f;
            TimingJudgement judgement = EvaluateCatchDecisionJudgement();

            lastDecisionInputText = "L2";
            lastDecisionWindowStateText = judgement switch
            {
                TimingJudgement.Perfect => "PERFECT",
                TimingJudgement.Good => "GOOD",
                _ => "NONE"
            };

            sequenceUI?.FlashJudgement(judgement);

            ShowGlobalJudgementInfo("L2 REFLECT", judgement);

            if (debugLogs)
                Debug.Log($"[BoomerangLoopController] Catch decision L2 input. judgement={judgement}", this);

            //Si l2 se hace en good la secuencia falla 

            //////////////////////////////////////////////////////**************************************************/////////////////////////////////////////
            //if (judgement != TimingJudgement.Perfect)
            //{
            //    sequenceUI?.SetCatchPulseVisible(false);
            //    EnterFailCooldown("BAD INPUT");
            //    return;
            //}

            if (judgement == TimingJudgement.Perfect)
            {
                sequenceUI?.SetCatchPulseVisible(false);

                pendingDecisionRelease = false;
                pendingDecisionReleaseTime = 0f;
                pendingDecisionReleaseJudgement = default;

                PlayDecisionReflectMeleeVisual();

                if (TryActivateOrbitReward(BoomerangLoopRewardTriggerInput.OnReflectL2))
                    return;

                PerformPerfectReflectFromDecision();
                UpdateUI();
                return;
            }
        }

        if (pendingDecisionRelease)
        {
            float elapsed = Time.time - pendingDecisionReleaseTime;

            float graceSeconds = activeDefinition.ResolveDecisionReleaseToReflectGraceSeconds(rhythmClock);
            if (elapsed >= graceSeconds)
            {
                sequenceUI?.SetCatchPulseVisible(false);

                if (TryActivateOrbitReward(BoomerangLoopRewardTriggerInput.OnReleaseL1))
                {
                    pendingDecisionRelease = false;
                    pendingDecisionReleaseTime = 0f;
                    pendingDecisionReleaseJudgement = default;
                    return;
                }

                lastDecisionInputText = "L1 RELEASE (COMMIT)";
                lastDecisionWindowStateText = pendingDecisionReleaseJudgement switch
                {
                    TimingJudgement.Perfect => "PERFECT",
                    TimingJudgement.Good => "GOOD",
                    _ => "NONE"
                };

                PerformRelaunch(pendingDecisionReleaseJudgement);

                pendingDecisionRelease = false;
                pendingDecisionReleaseTime = 0f;
                pendingDecisionReleaseJudgement = default;

                UpdateUI();
                return;
            }
        }

        if (runtime.IsWindowExpired())
        {
            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Catch decision expired -> fail cooldown.", this);

            sequenceUI?.SetCatchPulseVisible(false);
            EnterFailCooldown("TOO LATE");
            return;
        }
    }

    private bool TryConsumeReflectDecisionInput(PlayerInputReader input)
    {
        if (input == null)
            return false;

        return input.ConsumeSecondaryFireRequest();
    }

    private void PerformPerfectReflectFromDecision()
    {
        

        if (activeProjectile == null || activeDefinition == null || activeWeapon == null)
            return;

        reflectPerfectCount++;
        reflectSuccessCount++;
        weightedScore += activeDefinition.ReflectScoreWeight;

        performanceTracker.CommitCurrentCycle();
        performanceTracker.BeginCycle(relaunchSuccessCount + reflectSuccessCount + 1);

        Vector2 aim = activeWeapon.CurrentAim;
        activeProjectile.LoopReflectFromMelee(aim);

        BeginRecallPhase(false);


        if (debugLogs)
            Debug.Log(
                $"[BoomerangLoopController] Perfect L2 reflect from catch decision. " +
                $"score={weightedScore:F2} " +
                $"loops={GetCompletedLoopCount()} " +
                $"reflects={reflectSuccessCount}",
                this);
    }

    private bool TryActivateOrbitReward(BoomerangLoopRewardTriggerInput trigger)
    {
        if (!IsRewardReady())
            return false;

        if (!CanTriggerRewardNow(trigger))
            return false;

        if (activeProjectile == null)
            return false;

        runtime.BeginOrbitReward();
        sequenceUI?.Hide();

        float duration = activeDefinition != null ? activeDefinition.OrbitDuration : 3f;
        activeProjectile.BeginOrbitReward(duration, 0);

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Orbit reward triggered by {trigger}.", this);

        return true;
    }

    private bool CanTriggerRewardNow(BoomerangLoopRewardTriggerInput trigger)
    {
        if (activeDefinition == null)
            return false;

        if (!activeDefinition.RequireExplicitRewardTrigger)
            return true;

        return activeDefinition.RewardTriggerInput == trigger;
    }

    private bool IsRewardReady()
    {
        if (debugRewardLogs)
        {
            Debug.Log(
                $"[BoomerangLoopController] IsRewardReady? " +
                $"score={weightedScore:F2}/{activeDefinition.RequiredWeightedScore:F2} " +
                $"loops={GetCompletedLoopCount()}/{activeDefinition.MinSuccessfulLoopsForReward} " +
                $"reflects={reflectSuccessCount}/{activeDefinition.MinReflectSuccessesForReward}",
                this);
        }

        if (activeDefinition == null || !activeDefinition.UseOrbitReward)
            return false;

        if (weightedScore < activeDefinition.RequiredWeightedScore)
            return false;

        if (activeDefinition.RequireSuccessfulLoopCount &&
            GetCompletedLoopCount() < activeDefinition.MinSuccessfulLoopsForReward)
            return false;

        if (reflectSuccessCount < activeDefinition.MinReflectSuccessesForReward)
            return false;

        SequenceRewardContextBase rewardContext = BuildLoopRewardContext();

        if (rewardContext == null)
            return true;

        SequenceRewardResolution resolution =
            rewardEvaluator.Evaluate(activeDefinition.RewardPolicy, rewardContext);

        if (activeDefinition.RewardPolicy == null)
        {
            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Reward ready: no extra policy assigned", this);

            return true;
        }

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Reward policy result: shouldApply={resolution.shouldApply}", this);

        return resolution.shouldApply;
    }

    private SequenceRewardContextBase BuildLoopRewardContext()
    {
        SequenceRewardContextBase context =
            rewardEvaluator.BuildContext(
                performanceTracker.Performance,
                GetCompletedLoopCount(),
                GetCompletedLoopCount());

        if (context == null)
            return null;

        context.SetInt("boomerang_loop_relaunch_successes", relaunchSuccessCount);
        context.SetInt("boomerang_loop_reflect_successes", reflectSuccessCount);
        context.SetInt("boomerang_loop_relaunch_perfects", relaunchPerfectCount);
        context.SetInt("boomerang_loop_reflect_perfects", reflectPerfectCount);
        context.SetFloat("boomerang_loop_weighted_score", weightedScore);
        context.SetInt("boomerang_loop_completed_loops", GetCompletedLoopCount());

        return context;
    }

    private void PlayDecisionReflectMeleeVisual()
    {
        if (!playDecisionReflectVisual)
            return;

        if (decisionReflectVisualData == null || decisionReflectVisualData.hitPrefab == null)
            return;

        if (decisionReflectSpawnPoint == null)
            return;

        Quaternion rotation =
            decisionReflectSpawnPoint.rotation *
            Quaternion.Euler(0f, 0f, decisionReflectVisualData.spriteAngleOffset);

        GameObject go = Instantiate(
            decisionReflectVisualData.hitPrefab,
            decisionReflectSpawnPoint.position,
            rotation);

        Collider2D[] colliders = go.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < colliders.Length; i++)
            colliders[i].enabled = false;

        float lifetime = Mathf.Max(0.01f, decisionReflectVisualData.hitLifetime);

        if (decisionReflectVisualData.attackAnimation != null)
        {
            lifetime = Mathf.Max(0.01f, decisionReflectVisualData.attackAnimation.length);
        }
        else
        {
            Animator animator = go.GetComponent<Animator>();
            if (animator == null)
                animator = go.GetComponentInChildren<Animator>(true);

            if (animator != null &&
                animator.runtimeAnimatorController != null &&
                animator.runtimeAnimatorController.animationClips.Length > 0)
            {
                lifetime = Mathf.Max(
                    0.01f,
                    animator.runtimeAnimatorController.animationClips[0].length);
            }
        }

        Destroy(go, lifetime);

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Spawned decision reflect melee visual. lifetime={lifetime:F3}", this);
    }

    private int GetCompletedLoopCount()
    {
        return relaunchSuccessCount + reflectSuccessCount;
    }


    private void TickRecovery()
    {
        if (activeProjectile == null)
        {
            ForceStop();
            return;
        }

        if (!runtime.CatchReached)
            return;

        if (runtime.IsWindowExpired())
            FinishAtCatch();
    }

    private void PerformRelaunch(TimingJudgement judgement)
    {
        if (activeProjectile == null || activeDefinition == null || activeWeapon == null)
            return;

        if (judgement == TimingJudgement.Perfect)
            relaunchPerfectCount++;

        relaunchSuccessCount++;
        weightedScore += activeDefinition.RelaunchScoreWeight;

        performanceTracker.CommitCurrentCycle();
        performanceTracker.BeginCycle(relaunchSuccessCount + reflectSuccessCount + 1);

        Vector2 aim = activeWeapon.CurrentAim;
        activeProjectile.Relaunch(aim);

        BeginRecallPhase(false);
        allowHeldRecallAfterDecision = true;
        heldRecallAfterDecisionGraceEndTime = Time.time + heldRecallAfterDecisionGraceSeconds;

        if (debugLogs)
            Debug.Log(
                $"[BoomerangLoopController] Relaunch success. " +
                $"judgement={judgement} " +
                $"score={weightedScore:F2} " +
                $"loops={GetCompletedLoopCount()} " +
                $"reflects={reflectSuccessCount}",
                this);
    }



    private void OnProjectileReachedHoldTarget(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile || activeDefinition == null)
            return;

        // Durante la fase outbound inicial o durante el redirect outbound,
        // no queremos entrar todavía en catch decision.
        if (runtime.Phase == BoomerangLoopSequencePhase.OutboundRecallWindow ||
            runtime.Phase == BoomerangLoopSequencePhase.ShotRedirectedOutbound)
        {
            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Catch reached ignored in outbound phase.", this);

            return;
        }

        runtime.MarkCatchReached();

        sequenceUI?.FlashCatchCue(new Color(0.55f, 1f, 0.55f, 0.9f));
        sequenceUI?.SetCatchPulseVisible(true);

        if (debugLogs)
            Debug.Log("[BoomerangLoopController] Catch reached.", this);

        lastDecisionInputText = "WAITING";
        lastDecisionWindowStateText = "NONE";

        if (runtime.Phase == BoomerangLoopSequencePhase.Recovery)
        {
            UpdateUI();
            return;
        }

        float decisionDuration = activeDefinition.ResolveCatchDecisionWindowDuration(rhythmClock);

        if (activeDefinition.WaitForNextBeatOnDecision)
        {
            waitingForDecisionBeat = true;
            runtime.BeginDecisionPendingBeat();

            catchDecisionStartTime = 0f;
            sawBoomerangHeldInDecision = false;

            lastDecisionInputText = "WAIT NEXT BEAT";
            lastDecisionWindowStateText = "DECISION PENDING";
        }
        else
        {
            waitingForDecisionBeat = false;
            runtime.BeginCatchDecisionWindow(decisionDuration);

            catchDecisionStartTime = Time.time;
            sawBoomerangHeldInDecision = false;

            lastDecisionInputText = "WAITING";
            lastDecisionWindowStateText = "NONE";
        }

        pendingDecisionRelease = false;
        pendingDecisionReleaseTime = 0f;
        pendingDecisionReleaseJudgement = default;

        releaseIntentBuffered = false;
        releaseIntentBufferedTime = 0f;
        reflectIntentBuffered = false;
        reflectIntentBufferedTime = 0f;

        UpdateUI();
    }

    private void OnProjectileFinished(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile)
            return;

        if (debugLogs)
            Debug.Log("[BoomerangLoopController] Projectile finished -> cleanup.", this);

        CleanupStateOnly();
    }

   

    private void OnOrbitRewardFinished(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile)
            return;

        CleanupStateOnly();
        Destroy(projectile.gameObject);
    }

    private void FinishAtCatch()
    {
        if (debugLogs)
        {
            Debug.Log(
                $"[BoomerangLoopController] FinishAtCatch. " +
                $"activeProjectile={(activeProjectile != null)} " +
                $"score={weightedScore:F2} " +
                $"loops={GetCompletedLoopCount()} " +
                $"reflects={reflectSuccessCount}",
                this);
        }

        if (activeProjectile == null)
        {
            CleanupStateOnly();
            return;
        }

        if (ShouldStartOrbitReward())
        {
            runtime.BeginOrbitReward();
            sequenceUI?.Hide();

            float duration = activeDefinition != null ? activeDefinition.OrbitDuration : 3f;
            activeProjectile.BeginOrbitReward(duration, 0);

            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Orbit reward started.", this);

            return;
        }
        if (debugLogs)
            Debug.Log("[BoomerangLoopController] FinishAtCatch -> no reward, destroying projectile.", this);
        Destroy(activeProjectile.gameObject);
        CleanupStateOnly();
    }

    private bool ShouldStartOrbitReward()
    {
        if (activeDefinition == null)
        {
            if (debugLogs) Debug.Log("[BoomerangLoopController] ShouldStartOrbitReward -> false (no active definition)", this);
            return false;
        }

        if (activeDefinition.RequireExplicitRewardTrigger)
        {
            if (debugLogs) Debug.Log("[BoomerangLoopController] ShouldStartOrbitReward -> false (explicit trigger required)", this);
            return false;
        }

        bool ready = IsRewardReady();

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] ShouldStartOrbitReward -> {ready}", this);

        return ready;
    }

    private void FailSequence()
    {
        if (activeProjectile != null)
            Destroy(activeProjectile.gameObject);

        CleanupStateOnly();
    }

    private void ForceStop()
    {
        if (activeProjectile != null)
            Destroy(activeProjectile.gameObject);

        CleanupStateOnly();
    }

    private void CleanupStateOnly()
    {
        if (debugLogs)
            Debug.Log("[BoomerangLoopController] CleanupStateOnly -> hiding UI.", this);

        UnbindProjectileEvents();

        runtime.Complete();

        activeProjectile = null;
        activeWeapon = null;
        activeDefinition = null;

        ResetLoopCounters();
        ResetDecisionState();
        ResetFailState();
        ClearGlobalRhythmContext();

        ClearBufferedIntents();

        sequenceUI?.Hide();
    }

    private void BindProjectileEvents()
    {
        if (activeProjectile == null)
            return;

        activeProjectile.onReachedHoldTarget += OnProjectileReachedHoldTarget;
        activeProjectile.onFinished += OnProjectileFinished;
        activeProjectile.onLost += OnProjectileLost;
        activeProjectile.onOrbitRewardFinished += OnOrbitRewardFinished;
    }

    private void UnbindProjectileEvents()
    {
        if (activeProjectile == null)
            return;

        activeProjectile.onReachedHoldTarget -= OnProjectileReachedHoldTarget;
        activeProjectile.onFinished -= OnProjectileFinished;
        activeProjectile.onLost -= OnProjectileLost;
        activeProjectile.onOrbitRewardFinished -= OnOrbitRewardFinished;
    }

    private void UpdateUI()
    {
        if (sequenceUI == null || activeDefinition == null)
            return;

        TimedSequenceActionRule rule = GetCurrentRule();
        string phaseLabel = GetCurrentPhaseLabel();
        float normalized = GetCurrentNormalizedTime();

        bool useNeutralBar =
            runtime.Phase == BoomerangLoopSequencePhase.ReturningHold ||
            runtime.Phase == BoomerangLoopSequencePhase.Recovery;

        string instructionText = GetCurrentInstructionText();

        UpdateDecisionUI(phaseLabel, normalized, rule, useNeutralBar);
        UpdatePerformanceUI(instructionText);
    }


    private void UpdateDecisionUI(string phaseLabel, float normalized, TimedSequenceActionRule rule, bool useNeutralBar)
    {
        sequenceUI.SetPlayerBarMarkerVisible(true);

        sequenceUI.SetBoomerangWindowProgress(
            normalized,
            relaunchSuccessCount + reflectSuccessCount,
            Mathf.CeilToInt(activeDefinition.RequiredWeightedScore),
            rule,
            phaseLabel,
            useNeutralBar);


        sequenceUI.SetBoomerangDecisionDebug(lastDecisionInputText, lastDecisionWindowStateText);

        bool decisionPerfectActive =
            runtime.Phase == BoomerangLoopSequencePhase.CatchDecisionWindow &&
            activeDefinition != null &&
            activeDefinition.CatchDecisionRule != null &&
            activeDefinition.CatchDecisionRule.AllowPerfect &&
            Mathf.Abs(normalized - 0.5f) <= activeDefinition.CatchDecisionRule.PerfectHalfWindowNormalized;

        sequenceUI.SetDecisionPerfectActive(decisionPerfectActive);

        if (runtime.Phase == BoomerangLoopSequencePhase.OutboundRecallWindow && pendingRecallShotRedirect)
        {
            sequenceUI.SetDecisionPerfectActive(true);
        }
    }


    private void UpdatePerformanceUI(string instructionText)
    {
        SequencePerformanceUISnapshot snapshot =
            performanceTracker.Performance.BuildGenericUISnapshot(
                currentProgress: Mathf.RoundToInt(weightedScore),
                requiredProgress: Mathf.CeilToInt(activeDefinition.RequiredWeightedScore),
                rewardEligible: weightedScore >= activeDefinition.RequiredWeightedScore);

        snapshot.metric1Label = "Hits";
        snapshot.metric1Value = performanceTracker.Performance.TotalHitEvents.ToString();

        snapshot.metric2Label = "Unique";
        snapshot.metric2Value = performanceTracker.Performance.TotalUniqueEnemiesDamaged.ToString();

        snapshot.metric3Label = "Relaunch";
        snapshot.metric3Value = relaunchSuccessCount.ToString();

        snapshot.metric4Label = "R2 Hits";
        snapshot.metric4Value = shotRedirectSuccessCount.ToString();

        snapshot.rewardLabel = "Orbit";
        snapshot.rewardStateText = instructionText;

        snapshot.rewardFormulaText =
             $"Score {weightedScore:F1}/{activeDefinition.RequiredWeightedScore:F1}\n" +
             $"Reflects {reflectSuccessCount}/{activeDefinition.MinReflectSuccessesForReward}\n" +
             $"R2 Hits {shotRedirectSuccessCount}\n" +
             $"Redirect {(pendingRecallShotRedirect ? "ARMED" : "OFF")}";

        bool rewardReady = IsRewardReady();

        snapshot.rewardResultText = rewardReady
            ? (activeDefinition.RequireExplicitRewardTrigger ? "ARMED" : "READY")
            : "LOCKED";

        sequenceUI.SetPerformanceSnapshot(snapshot);
    }

    private TimedSequenceActionRule GetCurrentRule()
    {
        if (activeDefinition == null)
            return null;

        return runtime.Phase switch
        {
            BoomerangLoopSequencePhase.RecallPendingBeat => activeDefinition.RecallRule,
            BoomerangLoopSequencePhase.OutboundRecallWindow => activeDefinition.RecallRule,
            BoomerangLoopSequencePhase.DecisionPendingBeat => activeDefinition.CatchDecisionRule,
            BoomerangLoopSequencePhase.CatchDecisionWindow => activeDefinition.CatchDecisionRule,
            _ => null
        };
    }

    private string GetCurrentPhaseLabel()
    {
        return runtime.Phase switch
        {
            BoomerangLoopSequencePhase.RecallPendingBeat =>
                isPostRedirectRecall
                    ? $"Recall {GetCurrentRecallAttemptLabel()}"
                    : (pendingRecallShotRedirect ? "Shoot" : $"Recall {GetCurrentRecallAttemptLabel()}"),

            BoomerangLoopSequencePhase.OutboundRecallWindow =>
                isPostRedirectRecall
                    ? $"Recall {GetCurrentRecallAttemptLabel()}"
                    : (pendingRecallShotRedirect ? "Shoot" : $"Recall {GetCurrentRecallAttemptLabel()}"),

            BoomerangLoopSequencePhase.ShotRedirectedOutbound => "Redirect",
            BoomerangLoopSequencePhase.ReturningHold => "Hold",
            BoomerangLoopSequencePhase.DecisionPendingBeat => "Decision",
            BoomerangLoopSequencePhase.CatchDecisionWindow => "Decision",
            BoomerangLoopSequencePhase.Recovery => "Recovery",
            BoomerangLoopSequencePhase.FailCooldown => "Failed",
            BoomerangLoopSequencePhase.OrbitReward => "Orbit",
            _ => "Boomerang"
        };
    }

    private string GetCurrentRecallAttemptLabel()
    {
        if (recallAttemptsTotal <= 0)
            return "1/1";

        int currentAttempt = Mathf.Clamp((recallAttemptsTotal - recallAttemptsRemaining) + 1, 1, recallAttemptsTotal);
        return $"{currentAttempt}/{recallAttemptsTotal}";
    }

    private float GetCurrentNormalizedTime()
    {
        switch (runtime.Phase)
        {
            case BoomerangLoopSequencePhase.RecallPendingBeat:
            case BoomerangLoopSequencePhase.DecisionPendingBeat:
                return 0f;

            case BoomerangLoopSequencePhase.OutboundRecallWindow:
            case BoomerangLoopSequencePhase.CatchDecisionWindow:
                {
                    bool useBeatSyncedMarker =
                        (runtime.Phase == BoomerangLoopSequencePhase.OutboundRecallWindow && activeDefinition != null && activeDefinition.UseBeatSteppedRecall) ||
                        (runtime.Phase == BoomerangLoopSequencePhase.CatchDecisionWindow && activeDefinition != null && activeDefinition.UseBeatBasedDecisionTiming);

                    if (useBeatSyncedMarker && globalRhythmContext != null)
                    {
                        GlobalRhythmBarController barController = globalRhythmContext.GetBarController();
                        if (barController != null)
                            return barController.GetBeatPhase01();
                    }

                    return runtime.GetWindowNormalizedTime();
                }

            default:
                return runtime.GetWindowNormalizedTime();
        }
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

    private string GetCurrentInstructionText()
    {
        return runtime.Phase switch
        {
            BoomerangLoopSequencePhase.RecallPendingBeat =>
                isPostRedirectRecall ? "WAIT BEAT / L1 RECALL" : "WAIT BEAT / L1 RECALL / R2 SHOOT",

            BoomerangLoopSequencePhase.OutboundRecallWindow =>
                isPostRedirectRecall ? "L1 RECALL" : "L1 RECALL / R2 SHOOT",

            BoomerangLoopSequencePhase.ShotRedirectedOutbound => "GET READY",
            BoomerangLoopSequencePhase.ReturningHold => "HOLD L1",
            BoomerangLoopSequencePhase.DecisionPendingBeat => "WAIT BEAT / RELEASE OR R2",
            BoomerangLoopSequencePhase.CatchDecisionWindow => "L1 GOOD / L2 PERFECT",
            BoomerangLoopSequencePhase.Recovery => "RECOVERY",
            BoomerangLoopSequencePhase.FailCooldown => string.IsNullOrEmpty(failReasonText) ? "FAILED" : failReasonText,
            BoomerangLoopSequencePhase.OrbitReward => "ORBIT",
            _ => "BOOMERANG"
        };
    }

    private void EnterFailCooldown(string reason)
    {
        if (activeDefinition == null)
        {
            FailSequence();
            return;
        }

        failReasonText = reason;
        runtime.BeginFailCooldown(activeDefinition.FailCooldownDuration);

        sequenceUI?.FlashJudgement(default);

        if (!activeDefinition.KeepUIVisibleDuringFailCooldown)
        {
            sequenceUI?.Hide();
        }
        else
        {
            UpdateUI();
        }

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Enter fail cooldown. reason={reason}", this);
    }

    private void TickFailCooldown()
    {
        if (runtime.IsWindowExpired())
        {
            FailSequence();
        }
    }
    private string GetDecisionWindowStateText()
    {
        if (runtime.Phase != BoomerangLoopSequencePhase.CatchDecisionWindow || activeDefinition == null)
            return "-";

        TimingJudgement currentJudgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.CatchDecisionRule);

        return currentJudgement switch
        {
            TimingJudgement.Perfect => "PERFECT",
            TimingJudgement.Good => "GOOD",
            _ => "NONE"
        };
    }

    private void ResetLoopCounters()
    {
        relaunchSuccessCount = 0;
        reflectSuccessCount = 0;
        relaunchPerfectCount = 0;
        reflectPerfectCount = 0;
        weightedScore = 0f;
        shotRedirectSuccessCount = 0;
        shotRedirectPerfectCount = 0;
    }

    private void ResetDecisionState()
    {
        pendingDecisionRelease = false;
        pendingDecisionReleaseTime = 0f;
        pendingDecisionReleaseJudgement = default;

        lastDecisionInputText = "-";
        lastDecisionWindowStateText = "-";

        pendingRecallShotRedirect = false;
        pendingRecallShotRedirectStartTime = 0f;
        pendingRecallShotRedirectJudgement = default;

        isPostRedirectRecall = false;

        recallAttemptsRemaining = 0;
        recallAttemptsTotal = 0;


    }

  

    private void ResetFailState()
    {
        failReasonText = string.Empty;
        suppressProjectileLostFail = false;
    }


    public bool HasPendingRecallShotRedirect()
    {
        return runtime.IsRunning &&
               runtime.Phase == BoomerangLoopSequencePhase.OutboundRecallWindow &&
               pendingRecallShotRedirect &&
               activeProjectile != null;
    }

    public bool TryResolveRecallShotRedirect(PlayerProjectile projectile, BoomerangProjectile2D boomerang, Vector2 hitPoint)
    {
        if (!HasPendingRecallShotRedirect())
            return false;

        if (projectile == null || boomerang == null)
            return false;

        if (boomerang != activeProjectile)
            return false;

        Vector2 currentDir = boomerang.CurrentDirection;
        Vector2 toHit = ((Vector2)hitPoint - (Vector2)boomerang.transform.position).normalized;

        float cross = currentDir.x * toHit.y - currentDir.y * toHit.x;
        float signedAngle = cross >= 0f
            ? activeDefinition.RecallShotRedirectAngleDegrees
            : -activeDefinition.RecallShotRedirectAngleDegrees;

        Vector2 redirectedDir = (Quaternion.Euler(0f, 0f, signedAngle) * currentDir).normalized;

        if (debugLogs)
        {
            Debug.Log(
                $"[BoomerangLoopController] Recall shot redirect resolved. " +
                $"judgement={pendingRecallShotRedirectJudgement} angle={signedAngle:F1}",
                this);
        }

        TimingJudgement redirectJudgement = pendingRecallShotRedirectJudgement;

        shotRedirectSuccessCount++;

        if (redirectJudgement == TimingJudgement.Perfect)
            shotRedirectPerfectCount++;

        pendingRecallShotRedirect = false;
        pendingRecallShotRedirectStartTime = 0f;
        pendingRecallShotRedirectJudgement = default;

        lastDecisionInputText = "R2 HIT";
        lastDecisionWindowStateText = redirectJudgement == TimingJudgement.Perfect
            ? "REDIRECT PERFECT"
            : "REDIRECT GOOD";

        sequenceUI?.FlashJudgement(redirectJudgement);
        sequenceUI?.FlashCatchCue(new Color(0.35f, 0.85f, 1f, 0.95f));

        isPostRedirectRecall = true;

        activeProjectile.ApplyShotRedirect(
     redirectedDir,
     activeDefinition.RecallShotRedirectDirectionBlend,
     activeDefinition.RecallShotRedirectBlendDuration,
     activeDefinition.RecallShotRedirectDamageRadiusMultiplier,
     activeDefinition.RecallShotRedirectAuraSpinSpeedDegPerSec);

        runtime.BeginShotRedirectedOutbound(activeDefinition.ShotRedirectOutboundDuration);
        UpdateUI();

        return true;
    }

    private void ApplyBoomerangWeaponContext()
    {
        if (globalRhythmContext == null)
            return;

        globalRhythmContext.SetWeaponHint(GlobalRhythmWeaponHint.Boomerang);
        globalBoomerangContextApplied = true;
    }

    private void ClearGlobalRhythmContext()
    {
        if (globalRhythmContext == null)
            return;

        globalRhythmContext.ClearPrompt();
        globalRhythmContext.SetWindowRule(null);

        GlobalRhythmBarController barController = globalRhythmContext.GetBarController();
        if (barController != null)
        {
            GlobalRhythmBarView barView = barController.GetBarView();
            if (barView != null)
                barView.HideHoldOverlay();
        }

        if (globalBoomerangContextApplied)
        {
            globalRhythmContext.SetWeaponHint(GlobalRhythmWeaponHint.Default);
            globalBoomerangContextApplied = false;
        }

        lastAppliedGlobalPrompt = GlobalRhythmPromptType.None;
    }

    private void UpdateGlobalRhythmContext()
    {
        if (globalRhythmContext == null)
            return;

        UpdateGlobalActionOverlay();

        UpdateGlobalWindowRuleForCurrentPhase();

        UpdateGlobalRecallAttemptText();

        if (!runtime.IsRunning)
        {
            if (lastAppliedGlobalPrompt != GlobalRhythmPromptType.None)
            {
                globalRhythmContext.ClearPrompt();
                lastAppliedGlobalPrompt = GlobalRhythmPromptType.None;
            }

            return;
        }

        GlobalRhythmPromptType prompt = ResolveGlobalPromptForCurrentPhase();

        if (prompt == lastAppliedGlobalPrompt)
            return;

        if (prompt == GlobalRhythmPromptType.None)
            globalRhythmContext.ClearPrompt();
        else
            globalRhythmContext.SetPrompt(prompt);

        lastAppliedGlobalPrompt = prompt;
    }

    private void UpdateGlobalHoldOverlay(GlobalRhythmBarController barController, GlobalRhythmBarView barView)
    {
        if (!runtime.IsRunning || runtime.Phase != BoomerangLoopSequencePhase.ReturningHold || activeDefinition == null)
        {
            barView.HideHoldOverlay();
            return;
        }

        float holdBeats;

        if (activeDefinition.UseBeatBasedDecisionTiming && rhythmClock != null)
            holdBeats = activeDefinition.ReturnHoldDurationBeats;
        else if (rhythmClock != null && rhythmClock.SecondsPerBeat > 0.0001f)
            holdBeats = activeDefinition.ResolveReturnHoldDuration(rhythmClock) / rhythmClock.SecondsPerBeat;
        else
            holdBeats = 1f;

        float elapsedBeats = Mathf.Clamp01(runtime.GetWindowNormalizedTime()) * Mathf.Max(0f, holdBeats);

        float startBeatLabel = globalHoldReleaseBeatOffset + holdBeats - elapsedBeats;
        float endBeatLabel = globalHoldReleaseBeatOffset - elapsedBeats;

        float leftOuterNormalized = barController.GetNormalizedXForBeatLabel(startBeatLabel, true);
        float leftInnerNormalized = barController.GetNormalizedXForBeatLabel(endBeatLabel, true);

        float rightInnerNormalized = barController.GetNormalizedXForBeatLabel(endBeatLabel, false);
        float rightOuterNormalized = barController.GetNormalizedXForBeatLabel(startBeatLabel, false);

        barView.ShowHoldOverlay(
            leftOuterNormalized,
            leftInnerNormalized,
            rightInnerNormalized,
            rightOuterNormalized,
            globalHoldOverlayColor);
    }

    private GlobalRhythmPromptType ResolveGlobalPromptForCurrentPhase()
    {
        switch (runtime.Phase)
        {
            case BoomerangLoopSequencePhase.ReturningHold:
                return GlobalRhythmPromptType.Hold;

            case BoomerangLoopSequencePhase.CatchDecisionWindow:
                return GlobalRhythmPromptType.Release;

            case BoomerangLoopSequencePhase.OutboundRecallWindow:
                return GlobalRhythmPromptType.None;

            case BoomerangLoopSequencePhase.Recovery:
            case BoomerangLoopSequencePhase.FailCooldown:
                return GlobalRhythmPromptType.Danger;

            default:
                return GlobalRhythmPromptType.None;
        }
    }

   

    private TimingJudgement EvaluateGlobalRhythmJudgement()
    {
        if (rhythmCombat == null)
            return default;

        RhythmInputResult result = rhythmCombat.RegisterAttack(CombatAction.Special);

        return result.quality switch
        {
            RhythmHitQuality.Perfect => TimingJudgement.Perfect,
            RhythmHitQuality.Good => TimingJudgement.Good,
            _ => default
        };
    }

    private TimingJudgement EvaluateCatchDecisionJudgement()
    {
        if (activeDefinition != null && activeDefinition.UseBeatBasedDecisionTiming && rhythmCombat != null)
            return EvaluateGlobalRhythmJudgement();

        return EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition != null ? activeDefinition.CatchDecisionRule : null);
    }

    private void UpdateGlobalDecisionOverlay(GlobalRhythmBarController barController, GlobalRhythmBarView barView)
    {
        if (!runtime.IsRunning || runtime.Phase != BoomerangLoopSequencePhase.CatchDecisionWindow || activeDefinition == null)
        {
            barView.HideHoldOverlay();
            return;
        }

        float decisionBeats;

        if (activeDefinition.UseBeatBasedDecisionTiming && rhythmClock != null)
            decisionBeats = activeDefinition.CatchDecisionWindowBeats;
        else if (rhythmClock != null && rhythmClock.SecondsPerBeat > 0.0001f)
            decisionBeats = activeDefinition.ResolveCatchDecisionWindowDuration(rhythmClock) / rhythmClock.SecondsPerBeat;
        else
            decisionBeats = 1f;

        float elapsedBeats = Mathf.Clamp01(runtime.GetWindowNormalizedTime()) * Mathf.Max(0f, decisionBeats);

        // La decisión va desde el beat de release (1) hasta el centro (0).
        float currentBeatLabel = Mathf.Max(0f, globalHoldReleaseBeatOffset - elapsedBeats);

        float leftOuterNormalized = barController.GetNormalizedXForBeatLabel(currentBeatLabel, true);
        float leftInnerNormalized = 0.5f;

        float rightInnerNormalized = 0.5f;
        float rightOuterNormalized = barController.GetNormalizedXForBeatLabel(currentBeatLabel, false);

        barView.ShowHoldOverlay(
            leftOuterNormalized,
            leftInnerNormalized,
            rightInnerNormalized,
            rightOuterNormalized,
            globalDecisionOverlayColor);
    }

    private void UpdateGlobalActionOverlay()
    {
        if (globalRhythmContext == null)
            return;

        GlobalRhythmBarController barController = globalRhythmContext.GetBarController();
        if (barController == null)
            return;

        GlobalRhythmBarView barView = barController.GetBarView();
        if (barView == null)
            return;

        if (!runtime.IsRunning || activeDefinition == null)
        {
            barView.HideHoldOverlay();
            return;
        }

        switch (runtime.Phase)
        {
            case BoomerangLoopSequencePhase.ReturningHold:
                UpdateGlobalHoldOverlay(barController, barView);
                break;

            case BoomerangLoopSequencePhase.CatchDecisionWindow:
                UpdateGlobalDecisionOverlay(barController, barView);
                break;

            default:
                barView.HideHoldOverlay();
                break;
        }
    }

    private void ShowGlobalJudgementInfo(string label, TimingJudgement judgement)
    {
        globalRhythmContext?.ShowJudgementInfo(label, judgement);
    }

    private TimingJudgement EvaluateRecallJudgement()
    {
        if (activeDefinition != null && activeDefinition.UseBeatBasedDecisionTiming && rhythmCombat != null)
            return EvaluateGlobalRhythmJudgement();

        return EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition != null ? activeDefinition.RecallRule : null);
    }


    private void UpdateGlobalWindowRuleForCurrentPhase()
    {
        if (globalRhythmContext == null || activeDefinition == null)
            return;

        TimedSequenceActionRule rule = runtime.Phase switch
        {
            BoomerangLoopSequencePhase.OutboundRecallWindow => activeDefinition.RecallRule,
            BoomerangLoopSequencePhase.CatchDecisionWindow => activeDefinition.CatchDecisionRule,
            _ => null
        };

        globalRhythmContext.SetWindowRule(rule);
    }

    private void BeginRecallPhase(bool postRedirect)
    {
        if (activeDefinition == null)
            return;

        isPostRedirectRecall = postRedirect;
        pendingRecallPostRedirect = postRedirect;

        recallAttemptsTotal = activeDefinition.ResolveRecallBeatOpportunities(postRedirect);
        recallAttemptsRemaining = recallAttemptsTotal;

        float duration = activeDefinition.ResolveRecallStepWindowDuration(rhythmClock, postRedirect);

        if (activeDefinition.WaitForNextBeatOnRecall)
        {
            waitingForRecallBeat = true;
            runtime.BeginRecallPendingBeat();

            lastDecisionInputText = "WAIT NEXT BEAT";
            lastDecisionWindowStateText = $"RECALL 1/{recallAttemptsTotal}";
        }
        else
        {
            waitingForRecallBeat = false;
            runtime.BeginRecallWindow(duration);

            lastDecisionInputText = "WAIT L1";
            lastDecisionWindowStateText = $"BEAT 1/{recallAttemptsTotal}";
        }

        UpdateUI();

    }

    private void TickRecallPendingBeat()
    {
        if (!waitingForRecallBeat || activeDefinition == null)
            return;

        if (!IsNearNextBeat())
            return;

        waitingForRecallBeat = false;

        float duration = activeDefinition.ResolveRecallStepWindowDuration(rhythmClock, pendingRecallPostRedirect);
        runtime.BeginRecallWindow(duration);



        lastDecisionInputText = "WAIT L1";
        lastDecisionWindowStateText = $"BEAT 1/{recallAttemptsTotal}";

        if (debugLogs)
            Debug.Log("[BoomerangLoopController] Recall pending -> recall window started on next beat.", this);

        UpdateUI();
    }

    private bool AdvanceRecallAttemptWindow()
    {
        if (activeDefinition == null || !activeDefinition.UseBeatSteppedRecall)
            return false;

        if (recallAttemptsRemaining <= 1)
            return false;

        recallAttemptsRemaining--;

        int currentAttempt = (recallAttemptsTotal - recallAttemptsRemaining) + 1;

        pendingRecallPostRedirect = isPostRedirectRecall;

        float duration = activeDefinition.ResolveRecallStepWindowDuration(rhythmClock, isPostRedirectRecall);

        if (activeDefinition.WaitForNextBeatOnRecall)
        {
            waitingForRecallBeat = true;
            runtime.BeginRecallPendingBeat();

            lastDecisionInputText = "WAIT NEXT BEAT";
            lastDecisionWindowStateText = $"RECALL {currentAttempt}/{recallAttemptsTotal}";
        }
        else
        {
            waitingForRecallBeat = false;
            runtime.BeginRecallWindow(duration);

            lastDecisionInputText = "WAIT L1";
            lastDecisionWindowStateText = $"BEAT {currentAttempt}/{recallAttemptsTotal}";
        }

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Recall stepped window advanced -> pending beat {currentAttempt}/{recallAttemptsTotal}", this);

        UpdateUI();
        return true;
    }

    private void UpdateGlobalRecallAttemptText()
    {
        if (globalRhythmContext == null)
            return;

        if (!runtime.IsRunning || runtime.Phase != BoomerangLoopSequencePhase.OutboundRecallWindow)
        {
            globalRhythmContext.SetPromptTextOverride(string.Empty);
            return;
        }

        globalRhythmContext.SetPromptTextOverride($"RECALL {GetCurrentRecallAttemptLabel()}");
    }

   

    private float GetSecondsUntilNextBeat()
    {
        if (rhythmClock == null || rhythmClock.SecondsPerBeat <= 0.0001f)
            return 0f;

        float phase = rhythmClock.GetBeatPhase01();
        float remaining01 = 1f - phase;

        if (remaining01 <= 0.0001f)
            return 0f;

        return remaining01 * rhythmClock.SecondsPerBeat;
    }

    private bool IsNearNextBeat(float toleranceSeconds = 0.02f)
    {
        return GetSecondsUntilNextBeat() <= Mathf.Max(0.001f, toleranceSeconds);
    }

    private void TickDecisionPendingBeat(PlayerInputReader input)
    {
        if (!waitingForDecisionBeat || activeDefinition == null)
            return;

        if (!IsNearNextBeat())
            return;

        waitingForDecisionBeat = false;

        float decisionDuration = activeDefinition.ResolveCatchDecisionWindowDuration(rhythmClock);
        runtime.BeginCatchDecisionWindow(decisionDuration);

        releaseIntentBuffered = false;
        releaseIntentBufferedTime = 0f;
        reflectIntentBuffered = false;
        reflectIntentBufferedTime = 0f;

        catchDecisionStartTime = Time.time;
        sawBoomerangHeldInDecision = input != null && input.BoomerangHeld;

        lastDecisionInputText = "WAITING";
        lastDecisionWindowStateText = "NONE";

        if (debugLogs)
            Debug.Log("[BoomerangLoopController] Decision pending -> decision window started on next beat.", this);

        UpdateUI();
    }




}