using UnityEngine;

public class PlayerMoveState : PlayerState
{
    public PlayerMoveState(PlayerStateMachine sm, PlayerContext ctx) : base(sm, ctx) { }

    public override void Enter()
    {
        ctx.Movement.ReleaseVelocityOverride();
        ctx.Combat.SetCombatBlocked(false);
    }

    public override void Tick()
    {
        if (ctx.Input.ConsumeDashPressed())
        {
            Vector2 dashDir = ResolveDashDirection();
            sm.TryDash(dashDir);
            return;
        }

        if (ctx.Input.Move.sqrMagnitude <= 0.0001f)
            sm.GoIdle();
    }

    public override void FixedTick()
    {
        sm.UpdateLastNonZeroMove(ctx.Input.Move);
        ctx.Movement.Move(ctx.Input.Move.normalized);
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