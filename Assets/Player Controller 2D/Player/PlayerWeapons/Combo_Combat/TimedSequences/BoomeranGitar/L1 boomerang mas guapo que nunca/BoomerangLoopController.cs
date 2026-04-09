using UnityEngine;

public class BoomerangLoopController : MonoBehaviour, IBoomerangSequenceBridge
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController sequenceUI;
    [SerializeField] private WeaponBehaviour boomerangWeapon;
    [SerializeField] private Transform catchAnchor;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    [Header("Runtime")]
    [SerializeField] private BoomerangLoopSequenceRuntime runtime = new();
    [SerializeField] private BoomerangSequencePerformanceTracker performanceTracker = new();

    private BoomerangProjectile2D activeProjectile;
    private WeaponBehaviour activeWeapon;
    private BoomerangLoopWeaponDataSO activeWeaponData;
    private BoomerangLoopSequenceDefinitionSO activeDefinition;

    private BoomerangSequenceRewardEvaluator rewardEvaluator;

    private int relaunchSuccessCount;
    private int reflectSuccessCount;
    private int relaunchPerfectCount;
    private int reflectPerfectCount;
    private float weightedScore;

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

            case BoomerangLoopSequencePhase.ReturningHold:
                TickReturningHold(input);
                break;

            case BoomerangLoopSequencePhase.CatchReleaseWindow:
                TickCatchReleaseWindow(input);
                break;


            case BoomerangLoopSequencePhase.CatchHold:
                TickCatchHold();
                break;

            case BoomerangLoopSequencePhase.ReflectWindow:
                TickReflectWindow();
                break;

            case BoomerangLoopSequencePhase.Recovery:
                TickRecovery();
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
        activeWeaponData = weaponData;
        activeDefinition = weaponData.loopSequenceDefinition;

        relaunchSuccessCount = 0;
        reflectSuccessCount = 0;
        relaunchPerfectCount = 0;
        reflectPerfectCount = 0;
        weightedScore = 0f;

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

    public bool TryResolveMeleeReflect(BoomerangProjectile2D projectile, DeflectInfo info)
    {
        if (!runtime.IsRunning || projectile == null || projectile != activeProjectile || activeDefinition == null)
            return false;

        if (runtime.Phase != BoomerangLoopSequencePhase.ReflectWindow)
            return true;

        TimingJudgement judgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.ReflectRule);

        sequenceUI?.FlashJudgement(judgement);

        if (!IsSuccess(judgement))
        {
            FinishAtCatch();
            return true;
        }

        if (judgement == TimingJudgement.Perfect)
            reflectPerfectCount++;

        reflectSuccessCount++;
        weightedScore += activeDefinition.ReflectScoreWeight;

        performanceTracker.CommitCurrentCycle();
        performanceTracker.BeginCycle(relaunchSuccessCount + reflectSuccessCount + 1);

        //projectile.ReflectFromMelee(info.newDirection);
        projectile.LoopReflectFromMelee(info.newDirection);
        runtime.BeginRecallWindow(activeDefinition.RecallWindowDuration);
        UpdateUI();

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Reflect success. score={weightedScore:F2}", this);

        return true;
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

        if (runtime.IsWindowExpired())
        {
            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Recall window expired -> fail.", this);

            activeProjectile.EnterDriftLost();
            FailSequence();
            return;
        }

        if (!input.ConsumeBoomerangPressed())
            return;

        TimingJudgement judgement = EvaluateTiming(
            runtime.GetWindowNormalizedTime(),
            activeDefinition.RecallRule);

        sequenceUI?.FlashJudgement(judgement);

        if (!IsSuccess(judgement))
        {
            if (debugLogs)
                Debug.Log($"[BoomerangLoopController] Recall failed. judgement={judgement}", this);

            activeProjectile.EnterDriftLost();
            FailSequence();
            return;
        }

        if (debugLogs)
            Debug.Log($"[BoomerangLoopController] Recall success. judgement={judgement}", this);

        activeProjectile.StartCurvedReturn(activeDefinition.ReturnHoldDuration, 1f);
        runtime.BeginReturningHold(activeDefinition.ReturnHoldDuration);
        UpdateUI();
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
    }

    private void TickCatchReleaseWindow(PlayerInputReader input)
    {
        if (activeDefinition == null || activeProjectile == null)
        {
            ForceStop();
            return;
        }

        if (input.ConsumeBoomerangReleased())
        {
            TimingJudgement judgement = EvaluateTiming(
                 runtime.GetWindowNormalizedTime(),
                 activeDefinition.ReleaseRule); ;

            sequenceUI?.FlashJudgement(judgement);

            if (debugLogs)
                Debug.Log($"[BoomerangLoopController] Release input. judgement={judgement}", this);

            if (IsSuccess(judgement) && activeDefinition.AllowRelaunchBranch)
            {
                if (debugLogs)
                    Debug.Log($"[BoomerangLoopController] Release success -> relaunch. judgement={judgement}", this);
                
                sequenceUI?.SetCatchPulseVisible(false);
                PerformRelaunch(judgement);
                UpdateUI();
                return;
            }
        }

        if (runtime.IsWindowExpired())
        {
            if (debugLogs)
                Debug.Log("[BoomerangLoopController] Release window expired -> post catch flow.", this);
            sequenceUI?.SetCatchPulseVisible(false);
            EnterPostCatchFlow();
        }
    }

    

    private void TickCatchHold()
    {
        if (activeDefinition == null || activeProjectile == null)
        {
            ForceStop();
            return;
        }

        if (runtime.IsWindowExpired())
        {
            if (activeDefinition.AllowReflectBranch)
            {
                if (debugLogs)
                    Debug.Log("[BoomerangLoopController] Reflect window opened.", this);

                runtime.BeginReflectWindow(activeDefinition.ReflectWindowDuration);
            }
            else
            {
                FinishAtCatch();
            }

            UpdateUI();
        }
    }

    private void TickReflectWindow()
    {
        if (activeDefinition == null || activeProjectile == null)
        {
            ForceStop();
            return;
        }

        if (runtime.IsWindowExpired())
            FinishAtCatch();
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
            Debug.Log($"[BoomerangLoopController] Relaunch success. score={weightedScore:F2}", this);
    }

    private void EnterPostCatchFlow()
    {
        if (activeDefinition == null)
            return;

        if (activeDefinition.AllowReflectBranch && activeDefinition.ReflectDelayAfterCatch > 0f)
        {
            runtime.BeginCatchHold(activeDefinition.ReflectDelayAfterCatch);
        }
        else if (activeDefinition.AllowReflectBranch)
        {
            runtime.BeginReflectWindow(activeDefinition.ReflectWindowDuration);
        }
        else
        {
            FinishAtCatch();
        }

        UpdateUI();
    }

    private void OnProjectileReachedHoldTarget(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile || activeDefinition == null)
            return;

        runtime.MarkCatchReached();

        sequenceUI?.FlashCatchCue(new Color(0.55f, 1f, 0.55f, 0.9f));
        sequenceUI?.SetCatchPulseVisible(true);

        if (debugLogs)
            Debug.Log("[BoomerangLoopController] Catch reached.", this);

        if (runtime.Phase == BoomerangLoopSequencePhase.Recovery)
        {
            UpdateUI();
            return;
        }

        runtime.BeginCatchReleaseWindow(activeDefinition.CatchReleaseWindowDuration);
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

    private void OnProjectileLost(BoomerangProjectile2D projectile)
    {
        if (projectile != activeProjectile)
            return;

        if (debugLogs)
            Debug.Log("[BoomerangLoopController] Projectile lost -> fail.", this);

        FailSequence();
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

        Destroy(activeProjectile.gameObject);
        CleanupStateOnly();
    }

    private bool ShouldStartOrbitReward()
    {
        if (activeDefinition == null || !activeDefinition.UseOrbitReward)
            return false;

        if (weightedScore < activeDefinition.RequiredWeightedScore)
            return false;

        SequenceRewardContextBase rewardContext =
            rewardEvaluator.BuildContext(
                performanceTracker.Performance,
                relaunchSuccessCount + reflectSuccessCount,
                relaunchSuccessCount + reflectSuccessCount);

        if (rewardContext == null)
            return true;

        SequenceRewardResolution resolution =
            rewardEvaluator.Evaluate(activeDefinition.RewardPolicy, rewardContext);

        if (activeDefinition.RewardPolicy == null)
            return true;

        return resolution.shouldApply;
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
        activeWeaponData = null;
        activeDefinition = null;

        relaunchSuccessCount = 0;
        reflectSuccessCount = 0;
        relaunchPerfectCount = 0;
        reflectPerfectCount = 0;
        weightedScore = 0f;

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


        bool useNeutralBar = runtime.Phase == BoomerangLoopSequencePhase.ReturningHold ||
                             runtime.Phase == BoomerangLoopSequencePhase.Recovery;

        string instructionText = GetCurrentInstructionText();

        sequenceUI.SetBoomerangWindowProgress(
            normalized,
            relaunchSuccessCount + reflectSuccessCount,
            Mathf.CeilToInt(activeDefinition.RequiredWeightedScore),
            rule,
            phaseLabel,
            useNeutralBar);

      

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

        snapshot.metric4Label = "Reflect";
        snapshot.metric4Value = reflectSuccessCount.ToString();

        snapshot.rewardLabel = "Orbit";
        snapshot.rewardStateText = instructionText;
        snapshot.rewardFormulaText = $"Score {weightedScore:F1}/{activeDefinition.RequiredWeightedScore:F1}";
        snapshot.rewardResultText = weightedScore >= activeDefinition.RequiredWeightedScore ? "Ready" : "Locked";

        sequenceUI.SetPerformanceSnapshot(snapshot);
    }

    private TimedSequenceActionRule GetCurrentRule()
    {
        if (activeDefinition == null)
            return null;

        return runtime.Phase switch
        {
            BoomerangLoopSequencePhase.OutboundRecallWindow => activeDefinition.RecallRule,
            BoomerangLoopSequencePhase.CatchReleaseWindow => activeDefinition.ReleaseRule,
            BoomerangLoopSequencePhase.ReflectWindow => activeDefinition.ReflectRule,
            _ => null
        };
    }

    private string GetCurrentPhaseLabel()
    {
        return runtime.Phase switch
        {
            BoomerangLoopSequencePhase.OutboundRecallWindow => "Recall",
            BoomerangLoopSequencePhase.ReturningHold => "Hold",
            BoomerangLoopSequencePhase.CatchReleaseWindow => "Release",
            BoomerangLoopSequencePhase.CatchHold => "Catch",
            BoomerangLoopSequencePhase.ReflectWindow => "Reflect",
            BoomerangLoopSequencePhase.Recovery => "Recovery",
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
            BoomerangLoopSequencePhase.OutboundRecallWindow => "PRESS L1",
            BoomerangLoopSequencePhase.ReturningHold => "HOLD L1",
            BoomerangLoopSequencePhase.CatchReleaseWindow => "RELEASE L1",
            BoomerangLoopSequencePhase.CatchHold => "GET READY",
            BoomerangLoopSequencePhase.ReflectWindow => "PRESS L2",
            BoomerangLoopSequencePhase.Recovery => "RECOVERY",
            BoomerangLoopSequencePhase.OrbitReward => "ORBIT",
            _ => "BOOMERANG"
        };
    }
}