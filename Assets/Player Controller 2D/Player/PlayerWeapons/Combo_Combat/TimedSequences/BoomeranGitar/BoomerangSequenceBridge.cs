using System.Collections;
using UnityEngine;

public class BoomerangSequenceBridge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController sequenceUI;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private readonly BoomerangSequenceRuntime runtime = new BoomerangSequenceRuntime();

    private BoomerangProjectile2D activeProjectile;
    private WeaponBehaviour activeWeapon;
    private BoomerangWeaponDataSO activeWeaponData;
    private BoomerangSequenceDefinitionSO activeDefinition;

    private Coroutine phaseTransitionRoutine;
    private bool orbitRewardActive;

    public bool IsSequenceActive => runtime.IsRunning;
    public bool IsInOrbitReward => orbitRewardActive;
    public BoomerangSequencePhase ActivePhase => runtime.Phase;
    public int CompletedCycles => runtime.CompletedCycles;

    private void Awake()
    {
        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();

        if (sequenceUI == null)
            sequenceUI = GetComponentInChildren<TimedSequenceUIController>(true);
    }

    public bool BeginSequence(BoomerangProjectile2D projectile, WeaponBehaviour weapon, BoomerangWeaponDataSO weaponData)
    {
        if (projectile == null || weapon == null || weaponData == null || weaponData.sequenceDefinition == null)
            return false;

        if (!weaponData.sequenceDefinition.IsValid())
            return false;

        CancelActiveSequence(clearOverride: false, destroyProjectile: false);

        activeProjectile = projectile;
        activeWeapon = weapon;
        activeWeaponData = weaponData;
        activeDefinition = weaponData.sequenceDefinition;
        orbitRewardActive = false;

        activeProjectile.SetSequenceBridge(this);
        BindProjectileEvents(activeProjectile);

        runtime.Reset();
        runtime.BeginRecallWindow(activeDefinition.RecallWindowDuration);

        sequenceUI?.ShowBoomerang(activeDefinition, playerReferences);
        UpdateWindowUI();

        return true;
    }

    public void TickSequence(PlayerInputReader input)
    {
        if (!runtime.IsRunning || input == null || activeDefinition == null)
            return;

        if (phaseTransitionRoutine != null)
            return;

        // Cuando entra en órbita, la secuencia ya no secuestra el combate.
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
            projectile.ApplyNextReflectDashBoost(activeWeaponData.dashReflectSpeedMultiplierBonus);

        ForceWindowUIToEnd(activeDefinition.ReflectRule, "Reflect", false);
        phaseTransitionRoutine = StartCoroutine(ResolveReflectAfterUiHold(info.newDirection));
        return true;
    }

    public void CancelActiveSequence(bool clearOverride, bool destroyProjectile)
    {
        if (phaseTransitionRoutine != null)
        {
            StopCoroutine(phaseTransitionRoutine);
            phaseTransitionRoutine = null;
        }

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

        runtime.Reset();
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
            onSuccess: OnRecallDashSuccess,
            failReason: "Bad dash timing during recall window.");

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

        if (input.ConsumePrimaryFireRequest())
        {
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
            phaseTransitionRoutine = StartCoroutine(BeginReturnAfterUiHold());
        }
    }

    private IEnumerator BeginReturnAfterUiHold()
    {
        float hold = activeDefinition != null ? activeDefinition.UiPhaseTransitionHoldDuration : 0f;
        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        runtime.CompleteRecall();
        runtime.BeginReturnToReflectZone(activeDefinition.ReturnToReflectDuration);

        activeProjectile.StartCurvedReturn(
            activeDefinition.ReturnToReflectDuration,
            activeDefinition.ReflectActivationNormalized);

        UpdateWindowUI();
        phaseTransitionRoutine = null;
    }

    private IEnumerator ResolveReflectAfterUiHold(Vector2 reflectDirection)
    {
        float hold = activeDefinition != null ? activeDefinition.UiPhaseTransitionHoldDuration : 0f;
        if (hold > 0f)
            yield return new WaitForSeconds(hold);

        runtime.CompleteReflect();
        activeProjectile.ReflectFromMelee(reflectDirection);

        if (runtime.CompletedCycles >= activeDefinition.RequiredSuccessfulCycles)
        {
            StartOrbitRewardOrComplete();
        }
        else
        {
            runtime.LoopBackToRecallWindow(activeDefinition.RecallWindowDuration);
            UpdateWindowUI();
        }

        phaseTransitionRoutine = null;
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
            onSuccess: OnRecallDashSuccess,
            failReason: "Bad dash timing while returning to reflect zone.");

        if (!runtime.IsRunning)
            return;

        if (runtime.IsWindowExpired())
        {
            ForceWindowUIToEnd(null, "Return", true);
            sequenceUI?.FlashJudgement(default);
            activeProjectile.EnterDriftLost();
            FailSequence("Boomerang did not reach reflect zone in time.");
        }
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
            onSuccess: OnReflectDashSuccess,
            failReason: "Bad dash timing during reflect window.");

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

        TimingJudgement dashJudgement = EvaluateTiming(runtime.GetWindowNormalizedTime(), activeDefinition.DashRule);
        sequenceUI?.FlashJudgement(dashJudgement);

        if (IsSuccess(dashJudgement))
        {
            onSuccess?.Invoke();
        }
        else if (activeDefinition.FailOnBadDash)
        {
            FailSequence(failReason);
        }
    }

    private void OnRecallDashSuccess()
    {
        runtime.RegisterRecallDashSuccess();

        if (activeProjectile != null && activeWeaponData != null)
        {
            activeProjectile.ApplyReturnDashBoost(
                activeWeaponData.dashReturnSpeedMultiplierBonus,
                activeWeaponData.dashReturnSteeringBonus);
        }
    }

    private void OnReflectDashSuccess()
    {
        runtime.RegisterReflectDashSuccess();

        if (activeProjectile != null && activeWeaponData != null)
            activeProjectile.ApplyNextReflectDashBoost(activeWeaponData.dashReflectSpeedMultiplierBonus);
    }

    private void StartOrbitRewardOrComplete()
    {
        if (activeDefinition != null &&
            activeDefinition.UseOrbitReward &&
            activeProjectile != null &&
            activeWeaponData != null)
        {
            runtime.BeginOrbitReward();
            orbitRewardActive = true;

            // La UI de secuencia desaparece.
            sequenceUI?.Hide();

            // IMPORTANTÍSIMO:
            // devolvemos el arma base al jugador para que pueda volver a disparar/melee normal
            playerReferences?.Combat?.CancelAllAttacks();
            playerReferences?.WeaponOverride?.ClearActiveOverride();

            activeProjectile.BeginOrbitReward(
                activeDefinition.OrbitDuration,
                activeDefinition.OrbitTurns);

            return;
        }

        CompleteSequence();
    }

    private void CompleteSequence()
    {
        runtime.Complete();

        sequenceUI?.Hide();

        // aseguramos restauración del arma base al terminar
        playerReferences?.Combat?.CancelAllAttacks();
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        UnbindProjectileEvents();

        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;
        orbitRewardActive = false;
    }

    private void FailSequence(string reason)
    {
        if (debugLogs)
            Debug.LogWarning($"[BoomerangSequenceBridge] Fail -> {reason}", this);

        if (phaseTransitionRoutine != null)
        {
            StopCoroutine(phaseTransitionRoutine);
            phaseTransitionRoutine = null;
        }

        runtime.Fail();

        bool clearOverride = activeDefinition == null || activeDefinition.ClearWeaponOverrideOnFail;

        UnbindProjectileEvents();

        sequenceUI?.Hide();

        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;
        orbitRewardActive = false;

        if (clearOverride)
            playerReferences?.WeaponOverride?.ClearActiveOverride();
    }

    private void UpdateWindowUI()
    {
        if (sequenceUI == null || activeDefinition == null)
            return;

        bool useNeutralBar = runtime.Phase == BoomerangSequencePhase.ReturningToReflectZone;

        sequenceUI.SetBoomerangWindowProgress(
            runtime.GetWindowNormalizedTime(),
            runtime.CompletedCycles,
            activeDefinition.RequiredSuccessfulCycles,
            GetActiveBarRule(),
            GetPhaseLabel(),
            useNeutralBar);
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
            CompleteSequence();
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

        CompleteSequence();
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
}