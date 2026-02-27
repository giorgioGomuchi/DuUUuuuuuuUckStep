using UnityEngine;

public class PlayerIdleState : PlayerState
{
    public PlayerIdleState(PlayerStateMachine sm, PlayerContext ctx) : base(sm, ctx) { }

    public override void Enter()
    {
        ctx.Movement.ReleaseVelocityOverride();
        ctx.Combat.SetCombatBlocked(false);
    }

    public override void Tick()
    {
        // Dash request
        if (ctx.Input.ConsumeDashPressed())
        {
            Vector2 dashDir = ResolveDashDirection();
            sm.TryDash(dashDir);
            return;
        }

        // Movement transitions
        if (ctx.Input.Move.sqrMagnitude > 0.0001f)
            sm.GoMove();
    }

    public override void FixedTick()
    {
        // Idle: stop (o deja inercia si quieres)
        ctx.Movement.Move(Vector2.zero);
    }

    private Vector2 ResolveDashDirection()
    {
        // Prioridad: input actual -> last non-zero -> aim -> derecha
        if (ctx.Input.Move.sqrMagnitude > 0.0001f)
            return ctx.Input.Move.normalized;

        if (sm.LastNonZeroMoveDir.sqrMagnitude > 0.0001f)
            return sm.LastNonZeroMoveDir;

        if (ctx.Aim != null && ctx.Aim.CurrentAim.sqrMagnitude > 0.0001f)
            return ctx.Aim.CurrentAim.normalized;

        return Vector2.right;
    }
}