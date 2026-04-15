using UnityEngine;

public class BoomerangLoopController : MonoBehaviour, IBoomerangSequenceBridge
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController sequenceUI;
    [SerializeField] private WeaponBehaviour boomerangWeapon;
    [SerializeField] private Transform catchAnchor;

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

        if (!runtime.IsRunning)
        {
            TryStartFromIdle(input);
            return;
        }

        UpdateUI();

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

            case BoomerangLoopSequencePhase.FailCooldown:
                TickFailCooldown();
                break;
        }
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
        runtime.BeginRecallWindow(activeDefinition.RecallWindowDuration);

        sequenceUI?.ShowBoomerang(activeDefinition, playerReferences);
        UpdateUI();

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

        if (!input.ConsumeBoomerangPressed())
            return;

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
        if (!input.ConsumeBoomerangPressed())
            return;

        TimingJudgement recallJudgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.RecallRule);

        sequenceUI?.FlashJudgement(recallJudgement);

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

        activeProjectile.StartCurvedReturn(activeDefinition.ReturnHoldDuration, 1f);
        runtime.BeginReturningHold(activeDefinition.ReturnHoldDuration);
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

            runtime.BeginRecallWindow(activeDefinition.PostRedirectRecallWindowDuration);
            UpdateUI();
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

        TimingJudgement shotJudgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.RecallRule);

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
        if (input.ConsumeBoomerangReleased())
        {
            TimingJudgement judgement = EvaluateTiming(
                runtime.GetWindowNormalizedTime(),
                activeDefinition.CatchDecisionRule);

            lastDecisionInputText = "L1 RELEASE (QUEUED)";

            lastDecisionWindowStateText = judgement switch
            {
                TimingJudgement.Perfect => "PERFECT",
                TimingJudgement.Good => "GOOD",
                _ => "NONE"
            };

            sequenceUI?.FlashJudgement(judgement);

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
        if (TryConsumeReflectDecisionInput(input))
        {
            TimingJudgement judgement = EvaluateTiming(
                runtime.GetWindowNormalizedTime(),
                activeDefinition.CatchDecisionRule);

            lastDecisionInputText = "L2";
            lastDecisionWindowStateText = judgement switch
            {
                TimingJudgement.Perfect => "PERFECT",
                TimingJudgement.Good => "GOOD",
                _ => "NONE"
            };

            sequenceUI?.FlashJudgement(judgement);

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
            if (elapsed >= activeDefinition.DecisionReleaseToReflectGraceSeconds)
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

        runtime.BeginRecallWindow(activeDefinition.RecallWindowDuration);
        UpdateUI();


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

        runtime.BeginRecallWindow(activeDefinition.RecallWindowDuration);
        UpdateUI();

       
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

        runtime.BeginCatchDecisionWindow(activeDefinition.CatchDecisionWindowDuration);
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
        sequenceUI.SetBoomerangWindowProgress(
            normalized,
            relaunchSuccessCount + reflectSuccessCount,
            Mathf.CeilToInt(activeDefinition.RequiredWeightedScore),
            rule,
            phaseLabel,
            useNeutralBar);

        if (runtime.Phase == BoomerangLoopSequencePhase.CatchDecisionWindow)
            lastDecisionWindowStateText = GetDecisionWindowStateText();

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
            BoomerangLoopSequencePhase.OutboundRecallWindow => activeDefinition.RecallRule,
            BoomerangLoopSequencePhase.CatchDecisionWindow => activeDefinition.CatchDecisionRule,
            _ => null
        };
    }

    private string GetCurrentPhaseLabel()
    {
        return runtime.Phase switch
        {
            BoomerangLoopSequencePhase.OutboundRecallWindow =>
                isPostRedirectRecall ? "Recall 2" : (pendingRecallShotRedirect ? "Shoot" : "Recall"),

            BoomerangLoopSequencePhase.ShotRedirectedOutbound => "Redirect",
            BoomerangLoopSequencePhase.ReturningHold => "Hold",
            BoomerangLoopSequencePhase.CatchDecisionWindow => "Decision",
            BoomerangLoopSequencePhase.Recovery => "Recovery",
            BoomerangLoopSequencePhase.FailCooldown => "Failed",
            BoomerangLoopSequencePhase.OrbitReward => "Orbit",
            _ => "Boomerang"
        };
    }

    private float GetCurrentNormalizedTime()
    {
        return runtime.GetWindowNormalizedTime();
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
            BoomerangLoopSequencePhase.OutboundRecallWindow =>
                isPostRedirectRecall ? "L1 RECALL" : "L1 RECALL / R2 SHOOT",

            BoomerangLoopSequencePhase.ShotRedirectedOutbound => "GET READY",

            BoomerangLoopSequencePhase.ReturningHold => "HOLD L1",
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

}