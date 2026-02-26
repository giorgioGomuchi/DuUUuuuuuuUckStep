using UnityEngine;

public class PlayerDashState : PlayerState
{
    private Vector2 dashDir = Vector2.right;
    private float startTime;
    private float endTime;

    public PlayerDashState(PlayerStateMachine sm, PlayerContext ctx) : base(sm, ctx) { }

    public void SetDashDirection(Vector2 dir)
    {
        dashDir = (dir.sqrMagnitude > 0.0001f) ? dir.normalized : Vector2.right;
    }

    public override void Enter()
    {
        startTime = Time.time;
        endTime = startTime + sm.DashDuration;

        // Bloquea TODO (por defecto)
        ctx.Combat.SetCombatBlocked(true);
        ctx.Combat.CancelAllAttacks();

        // Invulnerabilidad
        if (ctx.Health != null)
            ctx.Health.SetInvulnerable(true);

        // Velocidad fija para recorrer distancia fija en duración fija
        float speed = sm.DashDistance / Mathf.Max(0.01f, sm.DashDuration);
        ctx.Movement.ForceVelocity(dashDir * speed);
    }

    public override void Exit()
    {
        ctx.Movement.ReleaseVelocityOverride();

        if (ctx.Health != null)
            ctx.Health.SetInvulnerable(false);

        // En exit no desbloqueo combate aquí; lo hará el siguiente estado (idle/move)
    }

    public override void Tick()
    {
        // Fin dash
        if (Time.time >= endTime)
        {
            GoPostDash();
            return;
        }

        // Cancel windows (solo si lo activas en booleans)
        float t = Mathf.InverseLerp(startTime, endTime, Time.time);
        if (t >= sm.DashCancelOpensAtNormalized)
        {
            // Opción: cancelar dash con acciones (para pruebas)
            // Por defecto todo false, así que el dash siempre es exclusivo.

            if (sm.AllowCancelDashWithPrimary && ctx.Input.FirePrimaryHeld)
            {
                // Aquí más adelante irías a AttackState; de momento salimos a Move/Idle.
                GoPostDash();
                return;
            }

            if (sm.AllowCancelDashWithSecondary && ctx.Input.FireSecondaryHeld)
            {
                GoPostDash();
                return;
            }

            if (sm.AllowCancelDashWithSwitchWeapon && ctx.Input.ConsumeSwitchWeaponPressed())
            {
                // Puedes permitir switch durante dash si quieres.
                GoPostDash();
                return;
            }
        }
    }

    public override void FixedTick()
    {
        // Mantener velocidad fija durante el dash (por si otras fuerzas intentan cambiarla)
        float speed = sm.DashDistance / Mathf.Max(0.01f, sm.DashDuration);
        ctx.Movement.ForceVelocity(dashDir * speed);
    }

    private void GoPostDash()
    {
        // Decide estado según input de movimiento
        if (ctx.Input.Move.sqrMagnitude > 0.0001f)
            sm.GoMove();
        else
            sm.GoIdle();
    }
}