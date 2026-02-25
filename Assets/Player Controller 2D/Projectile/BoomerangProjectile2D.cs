using UnityEngine;

public class BoomerangProjectile2D : KinematicProjectile2D
{
    private enum State { Outbound, Returning }

    [Header("Boomerang Config")]
    [SerializeField] private float outboundDistance = 6f;
    [SerializeField] private float returnSpeedMultiplier = 1.15f;
    [SerializeField] private bool deflectOnlyWhileReturning = true;
    [SerializeField] private float outboundDistanceAfterDeflect = 4.5f;
    [SerializeField] private float spinSpeed = 720f; // degrees per second

    [Header("Runtime")]
    [SerializeField] private Transform owner;

    private Vector2 startPos;
    private State state;

    public override bool CanBeDeflected
        => !deflectOnlyWhileReturning || state == State.Returning;

    public void ConfigureBoomerang(Transform owner, float outboundDistance, float returnSpeedMultiplier, bool deflectOnlyWhileReturning, float outboundDistanceAfterDeflect)
    {
        this.owner = owner;
        this.outboundDistance = Mathf.Max(0.1f, outboundDistance);
        this.returnSpeedMultiplier = Mathf.Max(0.1f, returnSpeedMultiplier);
        this.deflectOnlyWhileReturning = deflectOnlyWhileReturning;
        this.outboundDistanceAfterDeflect = Mathf.Max(0.1f, outboundDistanceAfterDeflect);

        startPos = rb.position;
        state = State.Outbound;

        if (debugLogs)
            Debug.Log($"[Boomerang] Configure owner={owner?.name} outbound={this.outboundDistance} returnMult={this.returnSpeedMultiplier}", this);
    }

    protected override void FixedUpdate()
    {
        transform.Rotate(0f, 0f, spinSpeed * Time.fixedDeltaTime);

        if (owner == null)
        {
            if (debugLogs) Debug.Log("[Boomerang] Owner missing -> Kill", this);
            Kill();
            return;
        }

        if (state == State.Outbound)
        {
            base.FixedUpdate();

            float dist = Vector2.Distance(startPos, rb.position);
            if (dist >= outboundDistance)
                SwitchToReturning();
        }
        else // Returning
        {
            Vector2 toOwner = ((Vector2)owner.position - rb.position);
            float dist = toOwner.magnitude;

            if (dist <= 0.25f)
            {
                if (debugLogs) Debug.Log("[Boomerang] Reached owner -> Kill", this);
                Kill();
                return;
            }

            Vector2 dir = toOwner.normalized;
            rb.MovePosition(rb.position + dir * (speed * returnSpeedMultiplier) * Time.fixedDeltaTime);

            // rotación visual
            direction = dir;
            RotateToDirection();
        }
    }

    private void SwitchToReturning()
    {
        state = State.Returning;
        if (debugLogs) Debug.Log("[Boomerang] Switch -> Returning", this);
    }

    public override void Deflect(DeflectInfo info)
    {
        if (!CanBeDeflected) return;

        // Regla boomerang: al deflect, vuelve a “salir” con nueva dirección, luego retornará otra vez
        targetLayerMask = info.newTargetMask;

        SetDirection(info.newDirection, rotate: true);
        SetSpeed(speed * info.speedMultiplier);

        startPos = rb.position;
        outboundDistance = outboundDistanceAfterDeflect;
        state = State.Outbound;

        if (debugLogs)
            Debug.Log($"[Boomerang] DEFLECT -> Outbound again dist={outboundDistance} newMask={targetLayerMask.value}", this);
    }

    protected override void OnHit(Collider2D other)
    {
        // Daño: igual que un proyectil normal pero sin matarlo al golpear (típico boomerang)
        // Si quieres que atraviese todo, así está bien. Si quieres que rebote en enemigos, sería otra regla.

        // Evitar autohit por owner (por si tiene colliders)
        if (owner != null && (other.transform == owner || other.transform.IsChildOf(owner)))
            return;

        if (!IsInTargetMask(other))
            return;

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
        {
            damageable.TakeDamage(damage);

            if (debugLogs)
                Debug.Log($"[Boomerang] Hit target={other.name} dmg={damage}", this);
        }
    }
}