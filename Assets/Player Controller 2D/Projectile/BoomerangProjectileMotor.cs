using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class BoomerangProjectileMotor
{
    private Rigidbody2D rb;
    private Transform owner;
    private BoomerangProjectileConfig config;

    private BoomerangProjectileMotorState state = BoomerangProjectileMotorState.None;

    private Vector2 direction = Vector2.right;
    private float speed;
    private int damage;
    private LayerMask targetMask;

    private Vector2 outboundStartPos;
    private float outboundLimitDistance;

    private float returnStartTime;
    private float returnDuration;
    private Vector2 returnStartPos;
    private Vector2 returnInitialDirection;

    private float runtimeReturnSteeringBonus;
    private float runtimeNextReflectSpeedMultiplier = 1f;

    private float driftSpeed;

    private float orbitAngleRad;
    private float orbitRadius;
    private float orbitAngularSpeedRad;
    private float orbitStartTime;
    private float orbitDuration;
    private int orbitTargetTurns;
    private float orbitAccumulatedRadians;
    private readonly Dictionary<int, float> orbitDamageCooldownByColliderId = new();

    private float redirectBlendSpeedBoost = 1.08f;

    private bool redirectBlendActive;
    private Vector2 redirectBlendStartDirection;
    private Vector2 redirectBlendTargetDirection;
    private float redirectBlendElapsed;
    private float redirectBlendDuration;

    public BoomerangProjectileMotorState State => state;
    public Vector2 Direction => direction;
    public float Speed => speed;
    public int Damage => damage;
    public LayerMask TargetMask => targetMask;
    public bool IsReflectable => state == BoomerangProjectileMotorState.ReflectHold;
    public bool IsOrbiting => state == BoomerangProjectileMotorState.Orbiting;

    public event Action OnBecameReflectable;
    public event Action OnReachedHoldTarget;
    public event Action OnOrbitFinished;
    public event Action OnLost;
    public event Action OnFinished;

    public void Initialize(
        Rigidbody2D rb,
        Transform owner,
        Vector2 initialDirection,
        float speed,
        int damage,
        LayerMask targetMask,
        BoomerangProjectileConfig config)
    {
        this.rb = rb;
        this.owner = owner;
        this.config = config ?? new BoomerangProjectileConfig();

        direction = initialDirection.sqrMagnitude > 0.0001f ? initialDirection.normalized : Vector2.right;
        this.speed = speed;
        this.damage = damage;
        this.targetMask = targetMask;

        state = BoomerangProjectileMotorState.Outbound;
        outboundStartPos = rb.position;
        outboundLimitDistance = this.config.outboundDistance;

        returnStartTime = 0f;
        returnDuration = 0f;
        returnStartPos = rb.position;
        returnInitialDirection = direction;

        runtimeReturnSteeringBonus = 0f;
        runtimeNextReflectSpeedMultiplier = 1f;

        driftSpeed = speed;
        orbitDamageCooldownByColliderId.Clear();

        redirectBlendActive = false;
        redirectBlendStartDirection = direction;
        redirectBlendTargetDirection = direction;
        redirectBlendElapsed = 0f;
        redirectBlendDuration = 0f;
    }

    public void Tick(float deltaTime, float fixedDeltaTime)
    {
        switch (state)
        {
            case BoomerangProjectileMotorState.Outbound:
            case BoomerangProjectileMotorState.ReflectedOutbound:
                TickOutbound(fixedDeltaTime);
                break;

            case BoomerangProjectileMotorState.Returning:
                TickReturning();
                break;

            case BoomerangProjectileMotorState.ReflectHold:
                TickReflectHold();
                break;

            case BoomerangProjectileMotorState.Orbiting:
                TickOrbit(fixedDeltaTime);
                break;

            case BoomerangProjectileMotorState.DriftingLost:
                TickDriftLost(fixedDeltaTime);
                break;
        }
    }

    public void LoopReflectRedirect(Vector2 newDirection, float initialBlend, float blendDuration)
    {
        Vector2 finalDirection = newDirection.sqrMagnitude > 0.0001f
            ? newDirection.normalized
            : (direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right);

        Vector2 startDirection = Vector2.Lerp(
            direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right,
            finalDirection,
            Mathf.Clamp01(initialBlend)).normalized;

        if (startDirection.sqrMagnitude <= 0.0001f)
            startDirection = finalDirection;

        direction = startDirection;
        speed *= Mathf.Max(0.01f, runtimeNextReflectSpeedMultiplier) * redirectBlendSpeedBoost;


        outboundStartPos = rb.position;
        outboundLimitDistance = Mathf.Max(0.1f, config.outboundDistance);

        runtimeReturnSteeringBonus = 0f;
        runtimeNextReflectSpeedMultiplier = 1f;

        redirectBlendActive = blendDuration > 0.0001f && Vector2.Dot(startDirection, finalDirection) < 0.9999f;
        redirectBlendStartDirection = startDirection;
        redirectBlendTargetDirection = finalDirection;
        redirectBlendElapsed = 0f;
        redirectBlendDuration = Mathf.Max(0.01f, blendDuration);

        state = BoomerangProjectileMotorState.ReflectedOutbound;
    }

    private void ResetRedirectBlend()
    {
        redirectBlendActive = false;
        redirectBlendStartDirection = direction;
        redirectBlendTargetDirection = direction;
        redirectBlendElapsed = 0f;
        redirectBlendDuration = 0f;
    }

    public void StartTimedReturn(float duration)
    {
        if (owner == null || rb == null)
            return;

        returnDuration = Mathf.Max(0.05f, duration);
        returnStartTime = Time.time;
        returnStartPos = rb.position;
        returnInitialDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;
        ResetRedirectBlend();
        state = BoomerangProjectileMotorState.Returning;
    }

    public void Reflect(Vector2 newDirection)
    {
        Vector2 finalDirection = newDirection.sqrMagnitude > 0.0001f
            ? newDirection.normalized
            : (direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right);

        direction = finalDirection;
        speed *= Mathf.Max(0.01f, runtimeNextReflectSpeedMultiplier);

        outboundStartPos = rb.position;
        outboundLimitDistance = Mathf.Max(0.1f, config.outboundDistanceAfterDeflect);

        runtimeReturnSteeringBonus = 0f;
        runtimeNextReflectSpeedMultiplier = 1f;

        ResetRedirectBlend();

        state = BoomerangProjectileMotorState.ReflectedOutbound;
    }

    public void BeginOrbit(float duration, int targetTurns)
    {
        if (owner == null || rb == null)
            return;

        orbitDuration = Mathf.Max(0.05f, duration);
        orbitTargetTurns = Mathf.Max(0, targetTurns);
        orbitStartTime = Time.time;
        orbitAccumulatedRadians = 0f;
        orbitDamageCooldownByColliderId.Clear();

        Vector2 ownerPos = owner.position;
        Vector2 fromOwner = rb.position - ownerPos;
        if (fromOwner.sqrMagnitude <= 0.0001f)
            fromOwner = Vector2.right * config.orbitStartRadius;

        orbitRadius = Mathf.Max(config.orbitStartRadius, fromOwner.magnitude);
        orbitAngleRad = Mathf.Atan2(fromOwner.y, fromOwner.x);

        float orbitAngularSpeedDeg =
            Mathf.Max(1f, config.orbitAngularSpeedDegPerSec * Mathf.Max(0.01f, config.orbitSpeedMultiplier));

        orbitAngularSpeedRad = orbitAngularSpeedDeg * Mathf.Deg2Rad;
        if (config.orbitClockwise)
            orbitAngularSpeedRad *= -1f;
        
        ResetRedirectBlend();

        state = BoomerangProjectileMotorState.Orbiting;
    }

    public void EnterDriftLost()
    {
        driftSpeed = Mathf.Max(0f, speed);
        state = BoomerangProjectileMotorState.DriftingLost;

        ResetRedirectBlend();

        OnLost?.Invoke();
    }

    public void ApplyReturnDashBoost(float extraSteering)
    {
        runtimeReturnSteeringBonus = Mathf.Max(runtimeReturnSteeringBonus, extraSteering);
    }

    public void ApplyNextReflectSpeedBoost(float multiplier)
    {
        runtimeNextReflectSpeedMultiplier = Mathf.Max(runtimeNextReflectSpeedMultiplier, multiplier);
    }

    public void SetTargetMask(LayerMask newMask)
    {
        targetMask = newMask;
    }

    public bool IsInTargetMask(Collider2D other)
    {
        return (targetMask.value & (1 << other.gameObject.layer)) != 0;
    }

    public bool CanDamageOrbitTarget(Collider2D other)
    {
        if (state != BoomerangProjectileMotorState.Orbiting)
            return true;

        int id = other.GetInstanceID();
        if (orbitDamageCooldownByColliderId.TryGetValue(id, out float nextTime) && Time.time < nextTime)
            return false;

        orbitDamageCooldownByColliderId[id] = Time.time + Mathf.Max(0.01f, config.orbitContactDamageInterval);
        return true;
    }

    private void TickOutbound(float fixedDeltaTime)
    {
        if (redirectBlendActive)
        {
            redirectBlendElapsed += fixedDeltaTime;
            float t = Mathf.Clamp01(redirectBlendElapsed / Mathf.Max(0.0001f, redirectBlendDuration));

            // Ease out suave: empieza curvando más y termina asentándose.
            float eased = 1f - Mathf.Pow(1f - t, 2f);

            Vector2 blendedDirection = Vector2.Lerp(
                redirectBlendStartDirection,
                redirectBlendTargetDirection,
                eased);

            if (blendedDirection.sqrMagnitude > 0.0001f)
                direction = blendedDirection.normalized;
            else
                direction = redirectBlendTargetDirection;

            if (t >= 1f)
                redirectBlendActive = false;
        }

        rb.MovePosition(rb.position + direction * speed * fixedDeltaTime);

        float traveled = Vector2.Distance(outboundStartPos, rb.position);
        if (traveled >= Mathf.Max(0.1f, outboundLimitDistance))
            EnterDriftLost();
    }

    private void TickReturning()
    {
        if (owner == null)
        {
            EnterDriftLost();
            return;
        }

        float elapsed = Time.time - returnStartTime;
        float normalized = Mathf.Clamp01(elapsed / Mathf.Max(0.0001f, returnDuration));

        Vector2 holdTarget = ResolveHoldTarget();
        Vector2 nextPos = EvaluateReturnPosition(normalized, holdTarget);

        Vector2 delta = nextPos - rb.position;
        if (delta.sqrMagnitude > 0.000001f)
            direction = delta.normalized;

        rb.MovePosition(nextPos);

        if (normalized >= 1f)
        {
            rb.position = holdTarget;
            state = BoomerangProjectileMotorState.ReflectHold;
            OnReachedHoldTarget?.Invoke();
            OnBecameReflectable?.Invoke();
        }
    }

    private void TickReflectHold()
    {
        if (owner == null)
        {
            EnterDriftLost();
            return;
        }

        Vector2 holdTarget = ResolveHoldTarget();
        Vector2 delta = holdTarget - rb.position;

        if (delta.sqrMagnitude > 0.000001f)
            direction = delta.normalized;

        rb.MovePosition(holdTarget);
    }

    private void TickOrbit(float fixedDeltaTime)
    {
        if (owner == null)
        {
            Finish();
            return;
        }

        float elapsed = Time.time - orbitStartTime;

        orbitAngleRad += orbitAngularSpeedRad * fixedDeltaTime;
        orbitAccumulatedRadians += Mathf.Abs(orbitAngularSpeedRad * fixedDeltaTime);

        orbitRadius = Mathf.Min(
            config.orbitMaxRadius,
            orbitRadius + config.orbitRadiusGrowthPerSecond * fixedDeltaTime);

        Vector2 ownerPos = owner.position;
        Vector2 offset = new(Mathf.Cos(orbitAngleRad), Mathf.Sin(orbitAngleRad));
        Vector2 nextPos = ownerPos + offset * orbitRadius;

        Vector2 delta = nextPos - rb.position;
        if (delta.sqrMagnitude > 0.000001f)
            direction = delta.normalized;

        rb.MovePosition(nextPos);

        bool durationReached = elapsed >= orbitDuration;
        bool turnsReached = orbitTargetTurns > 0 &&
                            orbitAccumulatedRadians >= orbitTargetTurns * Mathf.PI * 2f;

        if (durationReached || turnsReached)
            OnOrbitFinished?.Invoke();
    }

    private void TickDriftLost(float fixedDeltaTime)
    {
        driftSpeed = Mathf.Max(0f, driftSpeed - config.driftDeceleration * fixedDeltaTime);

        if (driftSpeed <= 0.01f)
        {
            Finish();
            return;
        }

        rb.MovePosition(rb.position + direction * driftSpeed * fixedDeltaTime);
    }

    private void Finish()
    {
        state = BoomerangProjectileMotorState.Finished;
        OnFinished?.Invoke();
    }

    private Vector2 ResolveHoldTarget()
    {
        if (owner == null)
            return rb.position;

        if (config.holdReflectAtOwnerCenter)
            return owner.position;

        Vector2 ownerPos = owner.position;
        Vector2 axis = ResolvePresentationAxis();
        return ownerPos + axis * config.timedReturnPresentationDistance;
    }

    private Vector2 ResolvePresentationAxis()
    {
        if (direction.sqrMagnitude > 0.0001f)
            return -direction.normalized;

        if (owner == null)
            return Vector2.right;

        Vector2 toOwner = (Vector2)owner.position - rb.position;
        if (toOwner.sqrMagnitude > 0.0001f)
            return toOwner.normalized;

        return Vector2.right;
    }

    private Vector2 EvaluateReturnPosition(float normalized, Vector2 holdTarget)
    {
        Vector2 p0 = returnStartPos;
        Vector2 p2 = holdTarget;

        Vector2 toTarget = p2 - p0;
        float dist = toTarget.magnitude;
        Vector2 axis = dist > 0.0001f ? toTarget / dist : Vector2.right;
        Vector2 perpendicular = new(-axis.y, axis.x);

        float arcBias = 1f + runtimeReturnSteeringBonus * 0.02f;
        float arcMagnitude = config.timedReturnArcStrength * dist * arcBias;

        float sign = Vector2.Dot(perpendicular, returnInitialDirection) >= 0f ? 1f : -1f;
        Vector2 p1 = Vector2.Lerp(p0, p2, 0.5f) + perpendicular * arcMagnitude * sign;

        float t = Mathf.SmoothStep(0f, 1f, normalized);
        return EvaluateQuadraticBezier(p0, p1, p2, t);
    }

    private static Vector2 EvaluateQuadraticBezier(Vector2 a, Vector2 b, Vector2 c, float t)
    {
        float omt = 1f - t;
        return omt * omt * a + 2f * omt * t * b + t * t * c;
    }

    public void Relaunch(Vector2 newDirection)
    {
        Vector2 finalDirection = newDirection.sqrMagnitude > 0.0001f
            ? newDirection.normalized
            : (direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right);

        direction = finalDirection;

        // Para relaunch NO usamos la distancia corta de deflect.
        outboundStartPos = rb.position;
        outboundLimitDistance = Mathf.Max(0.1f, config.outboundDistance);

        runtimeReturnSteeringBonus = 0f;
        runtimeNextReflectSpeedMultiplier = 1f;

        ResetRedirectBlend();

        state = BoomerangProjectileMotorState.Outbound;
    }

    public void LoopReflect(Vector2 newDirection)
    {
        Vector2 finalDirection = newDirection.sqrMagnitude > 0.0001f
            ? newDirection.normalized
            : (direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right);

        direction = finalDirection;
        speed *= Mathf.Max(0.01f, runtimeNextReflectSpeedMultiplier);

        outboundStartPos = rb.position;

        // Para el reflect del loop usamos una distancia suficientemente larga
        // para que el jugador tenga otro recall real.
        outboundLimitDistance = Mathf.Max(0.1f, config.outboundDistance);

        runtimeReturnSteeringBonus = 0f;
        runtimeNextReflectSpeedMultiplier = 1f;

        ResetRedirectBlend();

        state = BoomerangProjectileMotorState.ReflectedOutbound;
    }

}