using System;
using System.Collections.Generic;
using UnityEngine;

public class BoomerangProjectile2D : KinematicProjectile2D
{
    [Header("Boomerang Config")]
    [SerializeField] private float outboundDistance = 6f;
    [SerializeField] private float returnSpeedMultiplier = 1.15f;
    [SerializeField] private bool deflectOnlyWhileReturning = true;
    [SerializeField] private float outboundDistanceAfterDeflect = 4.5f;

    [Header("Return Shape")]
    [SerializeField] private float returnSteering = 8f;
    [SerializeField] private float reflectableDistance = 1.15f;
    [SerializeField] private float catchDistance = 0.25f;

    [Header("Timed Return")]
    [SerializeField] private float timedReturnArcStrength = 0.08f;
    [SerializeField] private float timedReturnPresentationDistance = 0.95f;
    [SerializeField] private float timedReturnMinSpeedMultiplier = 0.85f;
    [SerializeField] private float timedReturnMaxSpeedMultiplier = 2.75f;
    [SerializeField] private float timedReturnSpeedSmoothing = 18f;
    [SerializeField] private float timedReturnHoldRadius = 0.55f;
    [SerializeField] private float timedReturnReflectableRadius = 0.95f;

    [Header("Projectile Interaction")]
    [SerializeField] private LayerMask destroyEnemyProjectileMask;

    [Header("Orbit Reward")]
    [SerializeField] private float orbitStartRadius = 0.8f;
    [SerializeField] private float orbitRadiusGrowthPerSecond = 1f;
    [SerializeField] private float orbitMaxRadius = 3.5f;
    [SerializeField] private float orbitAngularSpeedDegPerSec = 360f;
    [SerializeField] private bool orbitClockwise = true;
    [SerializeField] private float orbitContactDamageInterval = 0.2f;
    [SerializeField] private float orbitSpeedMultiplier = 1.2f;

    [Header("Lost / Drift")]
    [SerializeField] private float driftDeceleration = 18f;

    [Header("Spin")]
    [SerializeField] private bool enableSpin = true;
    [SerializeField] private float spinDegPerSec = 720f;

    [Header("Runtime")]
    [SerializeField] private Transform owner;

    [Header("Feedback")]
    [SerializeField] private GameObject returnWindowVfxRoot;
    [SerializeField] private TrailRenderer trail;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color returningColor = Color.white;

    public Action<BoomerangProjectile2D> onFinished;
    public Action<BoomerangProjectile2D> onReturnedToOwner;
    public Action<BoomerangProjectile2D> onEnteredReturning;
    public Action<BoomerangProjectile2D> onBecameReflectable;
    public Action<BoomerangProjectile2D> onLost;
    public Action<BoomerangProjectile2D> onOrbitRewardFinished;
    public Action<BoomerangProjectile2D, BoomerangFlightState> onFlightStateChanged;

    private bool finishedNotified;
    private bool reflectableEventSent;
    private bool returnWindowActive;
    private Vector2 startPos;
    private BoomerangFlightState flightState = BoomerangFlightState.Outbound;

    private BoomerangSequenceBridge sequenceBridge;

    private float runtimeReturnSpeedMultiplierBonus;
    private float runtimeReturnSteeringBonus;
    private float runtimeNextReflectSpeedMultiplier = 1f;

    private Color baseColor = Color.white;
    private Color reflectableColor = Color.yellow;
    private float reflectableFlashDuration = 0.12f;
    private float reflectableFlashEndTime;

    private float timedReturnStartTime;
    private float timedReturnWindowDuration;
    private float reflectActivationNormalized = 0.55f;
    private bool useTimedReturn;
    private float currentTimedReturnSpeed;

    private float orbitAngleRad;
    private float orbitRadius;
    private float orbitAngularSpeedRad;
    private float orbitStartTime;
    private float orbitDuration;
    private int orbitTargetTurns;
    private float orbitAccumulatedRadians;

    private readonly Dictionary<int, float> orbitDamageCooldownByColliderId = new();

    public Transform Owner => owner;
    public BoomerangFlightState FlightState => flightState;
    public bool IsReflectable => flightState == BoomerangFlightState.ReflectableReturning;
    public bool IsOrbitRewardActive => flightState == BoomerangFlightState.OrbitingExpanding;

    public override bool CanBeDeflected
    {
        get
        {
            if (flightState == BoomerangFlightState.OrbitingExpanding)
                return false;

            if (!deflectOnlyWhileReturning)
            {
                return flightState != BoomerangFlightState.DriftingLost &&
                       flightState != BoomerangFlightState.Finished;
            }

            return flightState == BoomerangFlightState.ReflectableReturning;
        }
    }

    protected override void Awake()
    {
        base.Awake();

        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (trail == null)
            trail = GetComponentInChildren<TrailRenderer>();

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        SetReturnWindowActive(false);
    }

    private void Update()
    {
        UpdateVisualFeedback();
    }

    private void OnDisable()
    {
        NotifyFinished();
    }

    public void SetSequenceBridge(BoomerangSequenceBridge bridge)
    {
        sequenceBridge = bridge;
    }
    /*
     deflectOnlyWhileReturning: boom.deflectOnlyWhileReturning,
            outboundDistanceAfterDeflect: boom.outboundDistanceAfterDeflect,
     */
    public void ConfigureBoomerang(
     Transform owner,
     float outboundDistance,
     float returnSpeedMultiplier,
     bool deflectOnlyWhileReturning,
     float outboundDistanceAfterDeflect,
     float returnSteering,
     float reflectableDistance,
     float catchDistance,
     float driftDeceleration,
     float spinDegPerSec,
     Color reflectableColor,
     float reflectableFlashDuration,
     float timedReturnArcStrength,
     float timedReturnPresentationDistance,
     float timedReturnMinSpeedMultiplier,
     float timedReturnMaxSpeedMultiplier,
     float timedReturnSpeedSmoothing,
     float timedReturnHoldRadius,
     float timedReturnReflectableRadius,
     LayerMask destroyEnemyProjectileMask,
     float orbitStartRadius,
     float orbitRadiusGrowthPerSecond,
     float orbitMaxRadius,
     float orbitAngularSpeedDegPerSec,
     float orbitSpeedMultiplier,
     bool orbitClockwise,
     float orbitContactDamageInterval)
    {
        this.owner = owner;
        this.outboundDistance = Mathf.Max(0.1f, outboundDistance);
        this.returnSpeedMultiplier = Mathf.Max(0.1f, returnSpeedMultiplier);
        this.deflectOnlyWhileReturning = deflectOnlyWhileReturning;
        this.outboundDistanceAfterDeflect = Mathf.Max(0.1f, outboundDistanceAfterDeflect);
        this.returnSteering = Mathf.Max(0.1f, returnSteering);
        this.reflectableDistance = Mathf.Max(0.1f, reflectableDistance);
        this.catchDistance = Mathf.Max(0.05f, catchDistance);
        this.driftDeceleration = Mathf.Max(0f, driftDeceleration);
        this.spinDegPerSec = Mathf.Max(0f, spinDegPerSec);
        this.reflectableColor = reflectableColor;
        this.reflectableFlashDuration = Mathf.Max(0.01f, reflectableFlashDuration);

        this.timedReturnArcStrength = Mathf.Max(0f, timedReturnArcStrength);
        this.timedReturnPresentationDistance = Mathf.Max(0f, timedReturnPresentationDistance);
        this.timedReturnMinSpeedMultiplier = Mathf.Max(0.05f, timedReturnMinSpeedMultiplier);
        this.timedReturnMaxSpeedMultiplier = Mathf.Max(this.timedReturnMinSpeedMultiplier, timedReturnMaxSpeedMultiplier);
        this.timedReturnSpeedSmoothing = Mathf.Max(0.01f, timedReturnSpeedSmoothing);
        this.timedReturnHoldRadius = Mathf.Max(0.05f, timedReturnHoldRadius);
        this.timedReturnReflectableRadius = Mathf.Max(this.timedReturnHoldRadius, timedReturnReflectableRadius);

        this.destroyEnemyProjectileMask = destroyEnemyProjectileMask;

        this.orbitStartRadius = Mathf.Max(0.05f, orbitStartRadius);
        this.orbitRadiusGrowthPerSecond = Mathf.Max(0f, orbitRadiusGrowthPerSecond);
        this.orbitMaxRadius = Mathf.Max(this.orbitStartRadius, orbitMaxRadius);

        float finalOrbitAngularSpeed =
            Mathf.Max(1f, orbitAngularSpeedDegPerSec * Mathf.Max(0.01f, orbitSpeedMultiplier));

        this.orbitAngularSpeedRad = Mathf.Deg2Rad * finalOrbitAngularSpeed;

        if (orbitClockwise)
            this.orbitAngularSpeedRad *= -1f;

        this.orbitClockwise = orbitClockwise;
        this.orbitContactDamageInterval = Mathf.Max(0.01f, orbitContactDamageInterval);

        startPos = rb.position;
        finishedNotified = false;
        reflectableEventSent = false;

        runtimeReturnSpeedMultiplierBonus = 0f;
        runtimeReturnSteeringBonus = 0f;
        runtimeNextReflectSpeedMultiplier = 1f;
        reflectableFlashEndTime = 0f;
        currentTimedReturnSpeed = 0f;
        orbitDamageCooldownByColliderId.Clear();

        SetFlightState(BoomerangFlightState.Outbound);
        SetReturnWindowActive(false);
    }

    public void ApplyReturnDashBoost(float extraReturnSpeedMultiplier, float extraReturnSteering)
    {
        runtimeReturnSpeedMultiplierBonus = Mathf.Max(runtimeReturnSpeedMultiplierBonus, extraReturnSpeedMultiplier);
        runtimeReturnSteeringBonus = Mathf.Max(runtimeReturnSteeringBonus, extraReturnSteering);
        TriggerReflectableFlash();
    }

    public void ApplyNextReflectDashBoost(float reflectSpeedMultiplier)
    {
        runtimeNextReflectSpeedMultiplier = Mathf.Max(runtimeNextReflectSpeedMultiplier, reflectSpeedMultiplier);
        TriggerReflectableFlash();
    }

    public void BeginOrbitReward(float duration, int targetTurns)
    {
        if (owner == null)
            return;

        orbitDuration = Mathf.Max(0.05f, duration);
        orbitTargetTurns = Mathf.Max(0, targetTurns);
        orbitStartTime = Time.time;
        orbitAccumulatedRadians = 0f;
        orbitDamageCooldownByColliderId.Clear();

        Vector2 ownerPos = owner.position;
        Vector2 fromOwner = rb.position - ownerPos;
        if (fromOwner.sqrMagnitude <= 0.0001f)
            fromOwner = Vector2.right * orbitStartRadius;

        orbitRadius = Mathf.Max(orbitStartRadius, fromOwner.magnitude);
        orbitAngleRad = Mathf.Atan2(fromOwner.y, fromOwner.x);

        useTimedReturn = false;
        reflectableEventSent = false;
        SetReturnWindowActive(false);
        SetFlightState(BoomerangFlightState.OrbitingExpanding);
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

        switch (flightState)
        {
            case BoomerangFlightState.Outbound:
            case BoomerangFlightState.ReflectedOutbound:
                TickOutbound();
                break;

            case BoomerangFlightState.ReturningCurved:
                TickReturningCurved(canBecomeReflectable: true);
                break;

            case BoomerangFlightState.ReflectableReturning:
                TickReturningCurved(canBecomeReflectable: false);
                break;

            case BoomerangFlightState.OrbitingExpanding:
                TickOrbitReward();
                break;

            case BoomerangFlightState.DriftingLost:
                TickDriftingLost();
                break;
        }
    }

    public void StartCurvedReturn(float returnDuration, float reflectActivationNormalized)
    {
        reflectableEventSent = false;
        useTimedReturn = true;
        timedReturnStartTime = Time.time;
        timedReturnWindowDuration = Mathf.Max(0.05f, returnDuration);
        this.reflectActivationNormalized = Mathf.Clamp(reflectActivationNormalized, 0.05f, 0.95f);

        runtimeReturnSpeedMultiplierBonus = Mathf.Max(0f, runtimeReturnSpeedMultiplierBonus);
        runtimeReturnSteeringBonus = Mathf.Max(0f, runtimeReturnSteeringBonus);

        currentTimedReturnSpeed = Mathf.Max(
            0.01f,
            speed * (returnSpeedMultiplier + runtimeReturnSpeedMultiplierBonus));

        SetFlightState(BoomerangFlightState.ReturningCurved);
        SetReturnWindowActive(true);
        onEnteredReturning?.Invoke(this);
    }

    public void EnterDriftLost()
    {
        SetFlightState(BoomerangFlightState.DriftingLost);
        SetReturnWindowActive(false);
        onLost?.Invoke(this);
    }

    public void ReflectFromMelee(Vector2 newDirection)
    {
        if (newDirection.sqrMagnitude <= 0.0001f)
            newDirection = direction.sqrMagnitude > 0.0001f ? direction : Vector2.right;

        SetDirection(newDirection, rotate: true);
        SetSpeed(speed * Mathf.Max(0.01f, runtimeNextReflectSpeedMultiplier));

        runtimeNextReflectSpeedMultiplier = 1f;
        runtimeReturnSpeedMultiplierBonus = 0f;
        runtimeReturnSteeringBonus = 0f;
        useTimedReturn = false;
        currentTimedReturnSpeed = 0f;

        startPos = rb.position;
        reflectableEventSent = false;
        SetReturnWindowActive(false);
        SetFlightState(BoomerangFlightState.ReflectedOutbound);
    }

    public override void Deflect(DeflectInfo info)
    {
        if (!CanBeDeflected)
            return;

        targetLayerMask = info.newTargetMask;
        SetSpeed(speed * info.speedMultiplier);

        if (sequenceBridge != null && sequenceBridge.IsSequenceActive)
        {
            bool resolvedBySequence = sequenceBridge.TryResolveMeleeReflect(this, info);
            if (resolvedBySequence)
                return;
        }

        ReflectFromMelee(info.newDirection);
    }

    protected override void OnHit(Collider2D other)
    {
        if (other == null)
            return;

        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
            return;

        if (ShouldDestroyEnemyProjectile(other))
        {
            Destroy(other.gameObject);
            return;
        }

        if (!IsInTargetMask(other))
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null)
            return;

        if (flightState == BoomerangFlightState.OrbitingExpanding)
        {
            int id = other.GetInstanceID();
            if (orbitDamageCooldownByColliderId.TryGetValue(id, out float nextTime) && Time.time < nextTime)
                return;

            orbitDamageCooldownByColliderId[id] = Time.time + orbitContactDamageInterval;
        }

        damageable.TakeDamage(damage);
    }

    protected override void OnLifeTimeEnded()
    {
        NotifyFinished();
        base.OnLifeTimeEnded();
    }

    private bool ShouldDestroyEnemyProjectile(Collider2D other)
    {
        if ((destroyEnemyProjectileMask.value & (1 << other.gameObject.layer)) == 0)
            return false;

        KinematicProjectile2D projectile = other.GetComponent<KinematicProjectile2D>();
        if (projectile == null)
            projectile = other.GetComponentInParent<KinematicProjectile2D>();

        return projectile != null && projectile != this;
    }

    private void TickOutbound()
    {
        base.FixedUpdate();

        float dist = Vector2.Distance(startPos, rb.position);
        if (dist >= outboundDistance)
            EnterDriftLost();
    }

    private void TickReturningCurved(bool canBecomeReflectable)
    {
        if (useTimedReturn)
        {
            TickTimedReturn();
            return;
        }

        Vector2 ownerPos = owner.position;
        Vector2 currentPos = rb.position;
        Vector2 toOwner = ownerPos - currentPos;
        float distToOwner = toOwner.magnitude;

        if (distToOwner <= catchDistance)
        {
            NotifyReturnedToOwner();
            NotifyFinished();
            SetFlightState(BoomerangFlightState.Finished);
            Kill();
            return;
        }

        float finalSteering = returnSteering + runtimeReturnSteeringBonus;
        float baseReturnSpeed = speed * (returnSpeedMultiplier + runtimeReturnSpeedMultiplierBonus);

        Vector2 desiredDir = distToOwner > 0.0001f ? toOwner / distToOwner : Vector2.zero;
        direction = Vector2.Lerp(direction, desiredDir, finalSteering * Time.fixedDeltaTime).normalized;

        float moveDistance = Mathf.Max(0.01f, baseReturnSpeed) * Time.fixedDeltaTime;
        Vector2 fallbackNextPos = currentPos + direction * moveDistance;
        rb.MovePosition(fallbackNextPos);

        float fallbackNewDist = Vector2.Distance(fallbackNextPos, ownerPos);

        if (canBecomeReflectable && !reflectableEventSent && fallbackNewDist <= reflectableDistance)
        {
            reflectableEventSent = true;
            SetFlightState(BoomerangFlightState.ReflectableReturning);
            TriggerReflectableFlash();
            onBecameReflectable?.Invoke(this);
        }
    }

    private void TickTimedReturn()
    {
        Vector2 currentPos = rb.position;
        Vector2 targetPos = ResolveTimedReturnPresentationTarget();
        Vector2 toTarget = targetPos - currentPos;
        float distanceToTarget = toTarget.magnitude;

        float elapsed = Time.time - timedReturnStartTime;
        float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, timedReturnWindowDuration));
        float timeRemaining = Mathf.Max(0.02f, timedReturnWindowDuration - elapsed);

        float baseReturnSpeed = Mathf.Max(
            0.01f,
            speed * (returnSpeedMultiplier + runtimeReturnSpeedMultiplierBonus));

        float minSpeed = baseReturnSpeed * timedReturnMinSpeedMultiplier;
        float maxSpeed = baseReturnSpeed * timedReturnMaxSpeedMultiplier;
        float desiredSpeed = Mathf.Clamp(distanceToTarget / timeRemaining, minSpeed, maxSpeed);

        float acceleration = timedReturnSpeedSmoothing * baseReturnSpeed;
        currentTimedReturnSpeed = Mathf.MoveTowards(
            currentTimedReturnSpeed,
            desiredSpeed,
            acceleration * Time.fixedDeltaTime);

        float finalSteering = returnSteering + runtimeReturnSteeringBonus;
        Vector2 desiredDir = distanceToTarget > 0.0001f ? toTarget / distanceToTarget : direction;
        direction = Vector2.Lerp(direction, desiredDir, finalSteering * Time.fixedDeltaTime).normalized;

        bool canArmReflect =
            normalized >= reflectActivationNormalized &&
            distanceToTarget <= timedReturnReflectableRadius;

        if (!reflectableEventSent && canArmReflect)
        {
            reflectableEventSent = true;
            SetFlightState(BoomerangFlightState.ReflectableReturning);
            TriggerReflectableFlash();
            onBecameReflectable?.Invoke(this);
        }

        float moveDistance = Mathf.Max(0.01f, currentTimedReturnSpeed) * Time.fixedDeltaTime;

        if (reflectableEventSent && distanceToTarget <= timedReturnHoldRadius)
        {
            Vector2 nextPos = Vector2.MoveTowards(currentPos, targetPos, moveDistance);
            rb.MovePosition(nextPos);
            return;
        }

        Vector2 next = Vector2.MoveTowards(currentPos, targetPos, moveDistance);
        rb.MovePosition(next);
    }

    private Vector2 ResolveTimedReturnPresentationTarget()
    {
        Vector2 ownerPos = owner.position;
        Vector2 aimAxis = ResolveReturnPresentationAxis();
        Vector2 perpendicular = new Vector2(-aimAxis.y, aimAxis.x);

        Vector2 target = ownerPos - aimAxis * timedReturnPresentationDistance;

        float elapsed = Time.time - timedReturnStartTime;
        float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, timedReturnWindowDuration));

        float arcFalloff = 1f - normalized;
        float lateralDistance = Vector2.Distance(rb.position, ownerPos) * timedReturnArcStrength;
        target += perpendicular * lateralDistance * arcFalloff;

        return target;
    }

    private Vector2 ResolveReturnPresentationAxis()
    {
        Vector2 toOwner = (Vector2)owner.position - rb.position;
        if (toOwner.sqrMagnitude > 0.0001f)
            return toOwner.normalized;

        Vector2 axis = direction;
        if (axis.sqrMagnitude <= 0.0001f)
            axis = owner.right;
        if (axis.sqrMagnitude <= 0.0001f)
            axis = Vector2.right;

        return axis.normalized;
    }

    private void TickOrbitReward()
    {
        Vector2 ownerPos = owner.position;

        float previousAngle = orbitAngleRad;
        orbitAngleRad += orbitAngularSpeedRad * Time.fixedDeltaTime;
        orbitAccumulatedRadians += Mathf.Abs(orbitAngleRad - previousAngle);

        orbitRadius = Mathf.Min(
            orbitMaxRadius,
            orbitRadius + orbitRadiusGrowthPerSecond * Time.fixedDeltaTime);

        Vector2 offset = new Vector2(Mathf.Cos(orbitAngleRad), Mathf.Sin(orbitAngleRad)) * orbitRadius;
        Vector2 targetPos = ownerPos + offset;

        rb.MovePosition(targetPos);

        Vector2 tangent = new Vector2(-Mathf.Sin(orbitAngleRad), Mathf.Cos(orbitAngleRad));
        if (orbitClockwise)
            tangent *= -1f;

        if (tangent.sqrMagnitude > 0.0001f)
            SetDirection(tangent, rotate: false);

        bool durationReached = Time.time >= orbitStartTime + orbitDuration;
        bool turnsReached = orbitTargetTurns > 0 && orbitAccumulatedRadians >= orbitTargetTurns * Mathf.PI * 2f;

        if (durationReached || turnsReached)
        {
            onOrbitRewardFinished?.Invoke(this);
        }
    }

    private void TickDriftingLost()
    {
        if (speed > 0f)
            speed = Mathf.Max(0f, speed - driftDeceleration * Time.fixedDeltaTime);

        rb.MovePosition(rb.position + direction * speed * Time.fixedDeltaTime);

        if (speed <= 0.01f)
        {
            NotifyFinished();
            SetFlightState(BoomerangFlightState.Finished);
            Kill();
        }
    }

    private void TickSpin()
    {
        if (!enableSpin || Mathf.Approximately(spinDegPerSec, 0f))
            return;

        transform.Rotate(0f, 0f, spinDegPerSec * Time.fixedDeltaTime);
    }

    private void UpdateVisualFeedback()
    {
        if (spriteRenderer == null)
            return;

        if (flightState == BoomerangFlightState.ReflectableReturning)
        {
            spriteRenderer.color = reflectableFlashEndTime > Time.time ? reflectableColor : returningColor;
            return;
        }

        if (flightState == BoomerangFlightState.ReturningCurved ||
            flightState == BoomerangFlightState.OrbitingExpanding)
        {
            spriteRenderer.color = returningColor;
            return;
        }

        spriteRenderer.color = baseColor;
    }

    private void TriggerReflectableFlash()
    {
        reflectableFlashEndTime = Time.time + reflectableFlashDuration;
    }

    private void SetFlightState(BoomerangFlightState newState)
    {
        if (flightState == newState)
            return;

        flightState = newState;
        onFlightStateChanged?.Invoke(this, flightState);
    }

    private void SetReturnWindowActive(bool active)
    {
        if (returnWindowActive == active)
            return;

        returnWindowActive = active;

        if (returnWindowVfxRoot != null)
            returnWindowVfxRoot.SetActive(active);

        if (trail != null)
            trail.emitting = true;
    }

    private void NotifyReturnedToOwner()
    {
        onReturnedToOwner?.Invoke(this);
    }

    private void NotifyFinished()
    {
        if (finishedNotified)
            return;

        finishedNotified = true;
        onFinished?.Invoke(this);
    }

  
}