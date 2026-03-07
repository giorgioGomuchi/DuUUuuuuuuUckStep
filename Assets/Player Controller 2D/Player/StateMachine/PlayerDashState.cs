using UnityEngine;

public class PlayerDashState : PlayerState
{
    private Vector2 dashDir = Vector2.right;

    public PlayerDashState(PlayerStateMachine sm, PlayerContext ctx) : base(sm, ctx) { }

    public void SetDashDirection(Vector2 dir)
    {
        dashDir = dir.sqrMagnitude > 0.0001f ? dir.normalized : Vector2.right;
    }

    public override void Enter()
    {
        ctx.DashController.StartDash(dashDir);

        ctx.Combat.SetCombatBlocked(true);
        ctx.Combat.CancelAllAttacks();

        RecordDashAction();
    }

    public override void Exit()
    {
        ctx.DashController.StopDash();
    }

    public override void Tick()
    {
        ctx.DashController.Tick();

        if (!ctx.DashController.IsDashing)
        {
            GoPostDash();
            return;
        }

        float normalizedTime = ctx.DashController.GetNormalizedTime();

        if (normalizedTime >= sm.DashCancelOpensAtNormalized)
        {
            if (sm.AllowCancelDashWithPrimary && ctx.Input.FirePrimaryHeld)
            {
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
                GoPostDash();
                return;
            }
        }
    }

    public override void FixedTick()
    {
        ctx.DashController.FixedTick();
    }

    private void GoPostDash()
    {
        if (ctx.Input.Move.sqrMagnitude > 0.0001f)
            sm.GoMove();
        else
            sm.GoIdle();
    }

    private void RecordDashAction()
    {
        if (ctx.ActionRecorder == null)
            return;

        Vector2 aimDir = ctx.Aim != null ? ctx.Aim.CurrentAim : Vector2.zero;

        PlayerActionData actionData = new PlayerActionData(
            PlayerActionType.Dash,
            dashDir,
            aimDir,
            "Dash");

        ctx.ActionRecorder.RecordAction(actionData);
    }
}