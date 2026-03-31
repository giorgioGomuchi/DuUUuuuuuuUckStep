using System;
using UnityEngine;

public class BoomerangProjectile2D : KinematicProjectile2D
{
    [Header("Refs")]
    [SerializeField] private BoomerangProjectileVisuals visuals;
    [SerializeField] private BoomerangOwnerCollisionIgnore2D ownerCollisionIgnore;

    [Header("Runtime")]
    [SerializeField] private BoomerangProjectileMotor motor = new();

    public Action<BoomerangProjectile2D> onFinished;
    public Action<BoomerangProjectile2D> onReturnedToOwner;
    public Action<BoomerangProjectile2D> onEnteredReturning;
    public Action<BoomerangProjectile2D> onBecameReflectable;
    public Action<BoomerangProjectile2D> onLost;
    public Action<BoomerangProjectile2D> onOrbitRewardFinished;

    private Transform owner;
    private Transform ownerRoot;
    private BoomerangSequenceController sequenceBridge;
    private BoomerangProjectileConfig config;
    private bool finishedNotified;

    public Transform Owner => owner;
    public bool IsReflectable => motor.IsReflectable;
    public bool IsOrbitRewardActive => motor.IsOrbiting;

    public BoomerangFlightState FlightState
    {
        get
        {
            return motor.State switch
            {
                BoomerangProjectileMotorState.Outbound => BoomerangFlightState.Outbound,
                BoomerangProjectileMotorState.Returning => BoomerangFlightState.ReturningCurved,
                BoomerangProjectileMotorState.ReflectHold => BoomerangFlightState.ReflectableReturning,
                BoomerangProjectileMotorState.ReflectedOutbound => BoomerangFlightState.ReflectedOutbound,
                BoomerangProjectileMotorState.Orbiting => BoomerangFlightState.OrbitingExpanding,
                BoomerangProjectileMotorState.DriftingLost => BoomerangFlightState.DriftingLost,
                BoomerangProjectileMotorState.Finished => BoomerangFlightState.Finished,
                _ => BoomerangFlightState.Outbound
            };
        }
    }

    public override bool CanBeDeflected
    {
        get
        {
            if (config == null)
                return false;

            if (!config.deflectOnlyWhileReturning)
                return motor.State != BoomerangProjectileMotorState.DriftingLost &&
                       motor.State != BoomerangProjectileMotorState.Finished &&
                       motor.State != BoomerangProjectileMotorState.Orbiting;

            return motor.IsReflectable;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (visuals == null)
            visuals = GetComponentInChildren<BoomerangProjectileVisuals>(true);

        if (ownerCollisionIgnore == null)
            ownerCollisionIgnore = GetComponent<BoomerangOwnerCollisionIgnore2D>();

        HookMotorEvents();
        visuals?.ResetVisuals();
    }

    private void OnDisable()
    {
        ownerCollisionIgnore?.Restore();
        NotifyFinished();
    }

    public void SetSequenceBridge(BoomerangSequenceController bridge)
    {
        sequenceBridge = bridge;
    }

    public void Configure(Transform owner, BoomerangProjectileConfig config)
    {
        this.owner = owner;
        ownerRoot = owner != null ? owner.root : null;
        this.config = config ?? new BoomerangProjectileConfig();
        finishedNotified = false;

        ownerCollisionIgnore?.Apply(ownerRoot);

        visuals?.ApplyConfig(this.config);
        visuals?.ResetVisuals();

        motor.Initialize(
            rb,
            owner,
            direction,
            speed,
            damage,
            targetLayerMask,
            this.config);

        SyncVisualState();
        SetReturnWindowActive(false);
    }

    public void ApplyReturnDashBoost(float extraReturnSpeedMultiplier, float extraReturnSteering)
    {
        motor.ApplyReturnDashBoost(extraReturnSteering);
        TriggerReflectableFlash();
    }

    public void ApplyNextReflectDashBoost(float reflectSpeedMultiplier)
    {
        motor.ApplyNextReflectSpeedBoost(reflectSpeedMultiplier);
        TriggerReflectableFlash();
    }

    public void StartCurvedReturn(float duration, float reflectActivationNormalized)
    {
        motor.StartTimedReturn(duration);
        SyncVisualState();
        SetReturnWindowActive(true);
        onEnteredReturning?.Invoke(this);
    }

    public void BeginOrbitReward(float duration, int targetTurns)
    {
        motor.BeginOrbit(duration, targetTurns);
        SyncVisualState();
        SetReturnWindowActive(false);

        visuals?.TriggerOrbitStartFeedback(
            config.orbitStartFlashDuration,
            config.orbitStartPulseDuration);
    }

    public void EnterDriftLost()
    {
        motor.EnterDriftLost();
        SyncVisualState();
        SetReturnWindowActive(false);
    }

    public void ReflectFromMelee(Vector2 newDirection)
    {
        motor.Reflect(newDirection);
        SyncVisualState();
        SetReturnWindowActive(false);
    }

    public override void Deflect(DeflectInfo info)
    {
        if (!CanBeDeflected)
            return;

        motor.SetTargetMask(info.newTargetMask);
        SetSpeed(motor.Speed * info.speedMultiplier);

        if (sequenceBridge != null && sequenceBridge.IsSequenceActive)
        {
            bool resolvedBySequence = sequenceBridge.TryResolveMeleeReflect(this, info);
            if (resolvedBySequence)
                return;
        }

        ReflectFromMelee(info.newDirection);
    }

    protected override void FixedUpdate()
    {
        TickSpin();

        if (owner == null)
        {
            NotifyFinished();
            Kill();
            return;
        }

        motor.Tick(Time.deltaTime, Time.fixedDeltaTime);
        SyncVisualState();
    }

    protected override void OnHit(Collider2D other)
    {
        if (other == null)
            return;

        if (BelongsToOwnerBodyOnly(other))
            return;

        if (ShouldDestroyEnemyProjectile(other))
        {
            Destroy(other.gameObject);
            return;
        }

        if (!motor.IsInTargetMask(other))
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        if (!motor.CanDamageOrbitTarget(other))
            return;

        damageable.TakeDamage(motor.Damage);

        sequenceBridge?.RegisterBoomerangDamage(this, other, FlightState);
    }

    protected override void OnLifeTimeEnded()
    {
        NotifyFinished();
        base.OnLifeTimeEnded();
    }

    private void HookMotorEvents()
    {
        motor.OnBecameReflectable += HandleMotorBecameReflectable;
        motor.OnOrbitFinished += HandleMotorOrbitFinished;
        motor.OnLost += HandleMotorLost;
        motor.OnFinished += HandleMotorFinished;
    }

    private void HandleMotorBecameReflectable()
    {
        SyncVisualState();
        SetReturnWindowActive(false);
        TriggerReflectableFlash();
        onBecameReflectable?.Invoke(this);
    }

    private void HandleMotorOrbitFinished()
    {
        onOrbitRewardFinished?.Invoke(this);
    }

    private void HandleMotorLost()
    {
        SyncVisualState();
        onLost?.Invoke(this);
    }

    private void HandleMotorFinished()
    {
        SyncVisualState();
        NotifyFinished();
    }

    private void SyncVisualState()
    {
        visuals?.SetMotorState(motor.State);
    }

    private void SetReturnWindowActive(bool active)
    {
        visuals?.SetReturnWindowActive(active);
    }

    private void TriggerReflectableFlash()
    {
        visuals?.TriggerReflectableFlash(config.reflectableFlashDuration);
    }

    private void TickSpin()
    {
        if (config == null || !config.enableSpin || config.spinDegPerSec <= 0f)
            return;

        transform.Rotate(0f, 0f, config.spinDegPerSec * Time.fixedDeltaTime);
    }

    private void NotifyFinished()
    {
        if (finishedNotified)
            return;

        finishedNotified = true;
        onFinished?.Invoke(this);
    }

    private bool BelongsToOwnerBodyOnly(Collider2D other)
    {
        if (other == null || ownerRoot == null)
            return false;

        Transform t = other.transform;
        if (t != ownerRoot && !t.IsChildOf(ownerRoot))
            return false;

        int playerMeleeLayer = LayerMask.NameToLayer("PlayerMelee");
        if (playerMeleeLayer >= 0 && other.gameObject.layer == playerMeleeLayer)
            return false;

        return true;
    }

    private bool ShouldDestroyEnemyProjectile(Collider2D other)
    {
        if (config == null)
            return false;

        if ((config.destroyEnemyProjectileMask.value & (1 << other.gameObject.layer)) == 0)
            return false;

        KinematicProjectile2D projectile = other.GetComponent<KinematicProjectile2D>();
        if (projectile == null)
            projectile = other.GetComponentInParent<KinematicProjectile2D>();

        return projectile != null && projectile != this;
    }
}