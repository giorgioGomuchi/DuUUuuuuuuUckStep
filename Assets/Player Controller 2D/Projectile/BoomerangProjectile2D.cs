using System;
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

    private float timedReturnArcStrength;
    private float timedReturnCatchBias;

    private Vector2 timedReturnStartPos;
    private Vector2 timedReturnControlPoint;
    private Vector2 timedReturnInitialOwnerPos;

    public Action<BoomerangProjectile2D> onFinished;
    public Action<BoomerangProjectile2D> onReturnedToOwner;
    public Action<BoomerangProjectile2D> onEnteredReturning;
    public Action<BoomerangProjectile2D> onBecameReflectable;
    public Action<BoomerangProjectile2D> onLost;
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

    public Transform Owner => owner;
    public BoomerangFlightState FlightState => flightState;
    public bool IsReflectable => flightState == BoomerangFlightState.ReflectableReturning;


    // todo: FUTURE:
    // During boomerang sequence we currently allow the real secondary weapon/melee
    // to execute so the boomerang can be reflected by an actual hit.
    // Once the generic modular sequence system exists, this should migrate to a
    // unified SecondaryRule/action-context flow instead of relying on special-case
    // secondary passthrough inside PlayerCombatController.
    public override bool CanBeDeflected
    {
        get
        {
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
    float timedReturnCatchBias)
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
        this.timedReturnCatchBias = Mathf.Clamp(timedReturnCatchBias, 0.1f, 0.99f);

        startPos = rb.position;
        finishedNotified = false;
        reflectableEventSent = false;

        runtimeReturnSpeedMultiplierBonus = 0f;
        runtimeReturnSteeringBonus = 0f;
        runtimeNextReflectSpeedMultiplier = 1f;
        reflectableFlashEndTime = 0f;

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

            case BoomerangFlightState.DriftingLost:
                TickDriftingLost();
                break;
        }
    }

    public void StartCurvedReturn(float reflectWindowDuration, float reflectActivationNormalized)
    {
        reflectableEventSent = false;
        useTimedReturn = true;
        timedReturnStartTime = Time.time;
        timedReturnWindowDuration = Mathf.Max(0.05f, reflectWindowDuration);
        this.reflectActivationNormalized = Mathf.Clamp(reflectActivationNormalized, 0.05f, 0.95f);

        runtimeReturnSpeedMultiplierBonus = Mathf.Max(0f, runtimeReturnSpeedMultiplierBonus);
        runtimeReturnSteeringBonus = Mathf.Max(0f, runtimeReturnSteeringBonus);

        timedReturnStartPos = rb.position;
        timedReturnInitialOwnerPos = owner.position;

        Vector2 toOwner = timedReturnInitialOwnerPos - timedReturnStartPos;
        float distance = toOwner.magnitude;
        Vector2 dirToOwner = distance > 0.0001f ? toOwner / distance : Vector2.up;
        Vector2 perpendicular = new Vector2(-dirToOwner.y, dirToOwner.x);

        float arcAmount = distance * timedReturnArcStrength;
        timedReturnControlPoint = (timedReturnStartPos + timedReturnInitialOwnerPos) * 0.5f + perpendicular * arcAmount;

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

        // FUTURO:
        // Aquí convendrá sustituir esta resolución específica por una resolución
        // genérica de acción secundaria dentro del sistema modular común.
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
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
            return;

        if (!IsInTargetMask(other))
            return;

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(damage);
    }

    protected override void OnLifeTimeEnded()
    {
        NotifyFinished();
        base.OnLifeTimeEnded();
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

        if (useTimedReturn)
        {
            float elapsed = Time.time - timedReturnStartTime;
            float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.01f, timedReturnWindowDuration));

            // Mantiene el reflect sincronizado con la barra.
            float pathT = Mathf.Clamp01(normalized / Mathf.Max(0.001f, timedReturnCatchBias));

            Vector2 p0 = timedReturnStartPos;
            Vector2 p1 = timedReturnControlPoint;
            Vector2 p2 = ownerPos;

            Vector2 nextPos = EvaluateQuadraticBezier(p0, p1, p2, pathT);
            Vector2 moveDir = nextPos - currentPos;

            if (moveDir.sqrMagnitude > 0.000001f)
                direction = moveDir.normalized;

            rb.MovePosition(nextPos);

            float newDistToOwner = Vector2.Distance(nextPos, ownerPos);

            if (!reflectableEventSent && normalized >= reflectActivationNormalized)
            {
                reflectableEventSent = true;
                SetFlightState(BoomerangFlightState.ReflectableReturning);
                TriggerReflectableFlash();
                onBecameReflectable?.Invoke(this);
            }

            if (newDistToOwner <= catchDistance || normalized >= 1f)
            {
                NotifyReturnedToOwner();
                NotifyFinished();
                SetFlightState(BoomerangFlightState.Finished);
                Kill();
            }

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

    private static Vector2 EvaluateQuadraticBezier(Vector2 p0, Vector2 p1, Vector2 p2, float t)
    {
        float u = 1f - t;
        return (u * u * p0) + (2f * u * t * p1) + (t * t * p2);
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

        if (flightState == BoomerangFlightState.ReturningCurved)
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