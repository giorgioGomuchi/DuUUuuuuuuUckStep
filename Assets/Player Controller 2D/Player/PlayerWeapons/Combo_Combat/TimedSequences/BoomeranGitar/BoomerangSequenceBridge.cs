using UnityEngine;

public class BoomerangSequenceBridge : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private PlayerReferences playerReferences;
    [SerializeField] private TimedSequenceUIController sequenceUI;
    [SerializeField] private WeaponAimGuideController aimGuide;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private readonly BoomerangSequenceRuntime runtime = new BoomerangSequenceRuntime();

    private BoomerangProjectile2D activeProjectile;
    private WeaponBehaviour activeWeapon;
    private BoomerangWeaponDataSO activeWeaponData;
    private BoomerangSequenceDefinitionSO activeDefinition;

   

    public bool IsSequenceActive => runtime.IsRunning;
    public BoomerangSequencePhase ActivePhase => runtime.Phase;
    public int CompletedCycles => runtime.CompletedCycles;

    private void Awake()
    {
        if (playerReferences == null)
            playerReferences = GetComponentInParent<PlayerReferences>();

        if (sequenceUI == null)
            sequenceUI = GetComponentInChildren<TimedSequenceUIController>(true);

        if (aimGuide == null)
            aimGuide = GetComponentInChildren<WeaponAimGuideController>(true);
    }

    public bool BeginSequence(
    BoomerangProjectile2D projectile,
    WeaponBehaviour weapon,
    BoomerangWeaponDataSO weaponData)
    {
        Debug.Log("[BoomerangSequenceBridge] BeginSequence ENTER", this);

        if (projectile == null)
        {
            Debug.LogWarning("[BoomerangSequenceBridge] projectile is null", this);
            return false;
        }

        if (weapon == null)
        {
            Debug.LogWarning("[BoomerangSequenceBridge] weapon is null", this);
            return false;
        }

        if (weaponData == null)
        {
            Debug.LogWarning("[BoomerangSequenceBridge] weaponData is null", this);
            return false;
        }

        if (weaponData.sequenceDefinition == null)
        {
            Debug.LogWarning("[BoomerangSequenceBridge] sequenceDefinition is null", this);
            return false;
        }

        if (weaponData.sequenceDefinition.RecallRule == null)
        {
            Debug.LogWarning("[BoomerangSequenceBridge] RecallRule is null", this);
            return false;
        }

        if (weaponData.sequenceDefinition.ReflectRule == null)
        {
            Debug.LogWarning("[BoomerangSequenceBridge] ReflectRule is null", this);
            return false;
        }

        if (weaponData.sequenceDefinition.DashRule == null)
        {
            Debug.LogWarning("[BoomerangSequenceBridge] DashRule is null", this);
            return false;
        }

        if (!weaponData.sequenceDefinition.IsValid())
        {
            Debug.LogWarning("[BoomerangSequenceBridge] sequenceDefinition.IsValid() == false", this);
            return false;
        }

        CancelActiveSequence(clearOverride: false, destroyProjectile: false);

        activeProjectile = projectile;
        activeWeapon = weapon;
        activeWeaponData = weaponData;
        activeDefinition = weaponData.sequenceDefinition;

        activeProjectile.SetSequenceBridge(this);
        BindProjectileEvents(activeProjectile);

        runtime.Reset();
        runtime.BeginRecallWindow(activeDefinition.RecallWindowDuration);

        Debug.Log("[BoomerangSequenceBridge] Sequence STARTED OK", this);

        sequenceUI?.ShowBoomerang(activeDefinition, playerReferences);
        aimGuide?.ShowGuide();
        UpdateWindowUI();

        return true;
    }

    public void TickSequence(PlayerInputReader input)
    {
        if (!runtime.IsRunning || input == null || activeDefinition == null)
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

        // FUTURO:
        // Este punto debería migrar a una evaluación genérica de SecondaryRule
        // cuando el sistema modular unificado sustituya las secuencias específicas.
        if (!IsSuccess(judgement))
        {
            projectile.EnterDriftLost();
            FailSequence("Melee reflect outside reflect rule.");
            return true;
        }

        if (runtime.ReflectDashSucceededThisWindow && activeWeaponData != null)
        {
            projectile.ApplyNextReflectDashBoost(activeWeaponData.dashReflectSpeedMultiplierBonus);
        }

        runtime.CompleteReflect();
        projectile.ReflectFromMelee(info.newDirection);
        aimGuide?.FlashShot();

        if (debugLogs)
        {
            Debug.Log(
                $"[BoomerangSequenceBridge] Real melee reflect success ({judgement}) cycles={runtime.CompletedCycles}",
                this);
        }

        if (runtime.CompletedCycles >= activeDefinition.RequiredSuccessfulCycles)
        {
            CompleteSequence();
            return true;
        }

        runtime.LoopBackToRecallWindow(activeDefinition.RecallWindowDuration);
        UpdateWindowUI();
        return true;
    }

    public void CancelActiveSequence(bool clearOverride, bool destroyProjectile)
    {
        UnbindProjectileEvents();

        if (destroyProjectile && activeProjectile != null)
            Destroy(activeProjectile.gameObject);

        if (clearOverride)
            playerReferences?.WeaponOverride?.ClearActiveOverride();

        sequenceUI?.Hide();
        aimGuide?.HideGuide();

        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;
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

            runtime.CompleteRecall();

            // IMPORTANTE:
            // La ventana de reflect empieza inmediatamente después del recall correcto.
            runtime.BeginReflectWindow(activeDefinition.ReflectWindowDuration);

            activeProjectile.StartCurvedReturn(
                activeDefinition.ReflectWindowDuration,
                activeDefinition.ReflectActivationNormalized);

            aimGuide?.FlashShot();
            UpdateWindowUI();

            if (debugLogs)
                Debug.Log($"[BoomerangSequenceBridge] Recall success ({judgement}) -> ReflectWindow STARTED.", this);
        }
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
            sequenceUI?.FlashJudgement(default);
            activeProjectile.EnterDriftLost();
            FailSequence("Reflect timing expired.");
        }

        // IMPORTANTE:
        // El reflect ya no se resuelve con ConsumeSecondaryFireRequest().
        // Se resuelve por hit real de melee contra el boomerang.
        //
        // FUTURO:
        // Este comportamiento debería migrar a una SecondaryRule genérica
        // cuando el sistema modular común reemplace esta versión específica.
    }

    private void HandleDashInput(
        PlayerInputReader input,
        bool dashAllowed,
        System.Action onSuccess,
        string failReason)
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

            if (debugLogs)
                Debug.Log($"[BoomerangSequenceBridge] Dash success ({dashJudgement}).", this);
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

        if (activeProjectile != null)
            activeProjectile.ApplyNextReflectDashBoost(1.1f);
    }

    private void CompleteSequence()
    {
        runtime.Complete();

        sequenceUI?.Hide();
        aimGuide?.HideGuide();

        playerReferences?.Combat?.CancelAllAttacks();
        playerReferences?.WeaponOverride?.ClearActiveOverride();

        if (activeDefinition != null && activeDefinition.CompletionRewardWeaponData != null)
        {
            playerReferences?.WeaponOverride?.ApplyTemporaryWeaponOverride(
                activeDefinition.CompletionRewardSlot,
                activeDefinition.CompletionRewardWeaponData,
                activeDefinition.CompletionRewardAmmo);
        }

        UnbindProjectileEvents();
        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;
    }

    private void FailSequence(string reason)
    {
        if (debugLogs)
            Debug.LogWarning($"[BoomerangSequenceBridge] Fail -> {reason}", this);

        runtime.Fail();

        bool clearOverride = activeDefinition == null || activeDefinition.ClearWeaponOverrideOnFail;

        UnbindProjectileEvents();

        sequenceUI?.Hide();
        aimGuide?.HideGuide();

        activeProjectile = null;
        activeWeapon = null;
        activeWeaponData = null;
        activeDefinition = null;

        if (clearOverride)
            playerReferences?.WeaponOverride?.ClearActiveOverride();
    }

    private void UpdateWindowUI()
    {
        if (sequenceUI == null || activeDefinition == null)
            return;

        sequenceUI.SetBoomerangWindowProgress(
            runtime.GetWindowNormalizedTime(),
            runtime.CompletedCycles,
            activeDefinition.RequiredSuccessfulCycles,
            GetActiveBarRule(),
            GetPhaseLabel());
    }

    private TimedSequenceActionRule GetActiveBarRule()
    {
        return runtime.Phase switch
        {
            BoomerangSequencePhase.OutboundRecallWindow => activeDefinition.RecallRule,
            BoomerangSequencePhase.ReturningToReflectZone => activeDefinition.RecallRule,
            BoomerangSequencePhase.ReflectWindow => activeDefinition.ReflectRule,
            _ => activeDefinition.RecallRule
        };
    }

    private string GetPhaseLabel()
    {
        return runtime.Phase switch
        {
            BoomerangSequencePhase.OutboundRecallWindow => "Recall",
            BoomerangSequencePhase.ReturningToReflectZone => "Return",
            BoomerangSequencePhase.ReflectWindow => "Reflect",
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
    }

    private void UnbindProjectileEvents()
    {
        if (activeProjectile == null)
            return;

        activeProjectile.onBecameReflectable -= OnProjectileBecameReflectable;
        activeProjectile.onReturnedToOwner -= OnProjectileReturnedToOwner;
        activeProjectile.onFinished -= OnProjectileFinished;
        activeProjectile.onLost -= OnProjectileLost;
    }

    private void OnProjectileBecameReflectable(BoomerangProjectile2D projectile)
    {
        if (!runtime.IsRunning || projectile != activeProjectile)
            return;

        // Ya no abre aquí la ventana.
        // Solo sirve como feedback/evento físico de que el boomerang ya está en rango real de devolución.
        if (debugLogs)
            Debug.Log("[BoomerangSequenceBridge] Projectile became physically reflectable.", this);
    }

    private void OnProjectileReturnedToOwner(BoomerangProjectile2D projectile)
    {
        if (!runtime.IsRunning || projectile != activeProjectile)
            return;

        if (runtime.Phase == BoomerangSequencePhase.ReflectWindow)
            FailSequence("Boomerang returned before melee reflect.");
    }

    private void OnProjectileFinished(BoomerangProjectile2D projectile)
    {
        if (!runtime.IsRunning || projectile != activeProjectile)
            return;

        if (!runtime.IsCompleted)
            FailSequence("Projectile finished before sequence completion.");
    }

    private void OnProjectileLost(BoomerangProjectile2D projectile)
    {
        if (!runtime.IsRunning || projectile != activeProjectile)
            return;

        FailSequence("Boomerang lost.");
    }

    private static TimingJudgement EvaluateTiming(float normalizedTime, TimedSequenceActionRule rule)
    {
        if (rule == null)
            return default;

        float centerDistance = Mathf.Abs(Mathf.Clamp01(normalizedTime) - 0.5f);

        if (centerDistance <= rule.PerfectHalfWindowNormalized)
            return TimingJudgement.Perfect;

        if (centerDistance <= rule.GoodHalfWindowNormalized)
            return TimingJudgement.Good;

        return default;
    }

    private static bool IsSuccess(TimingJudgement judgement)
    {
        return judgement == TimingJudgement.Perfect ||
               judgement == TimingJudgement.Good;
    }
}