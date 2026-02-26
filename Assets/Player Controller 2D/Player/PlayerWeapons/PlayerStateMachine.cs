using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [Header("Dash Settings")]
    [SerializeField] private float dashDistance = 3.5f;
    [SerializeField] private float dashDuration = 0.12f;
    [SerializeField] private float dashCooldown = 0.45f;

    [Header("Dash Cancel (Testing Toggles)")]
    [SerializeField] private bool allowCancelDashWithPrimary = false;
    [SerializeField] private bool allowCancelDashWithSecondary = false;
    [SerializeField] private bool allowCancelDashWithSwitchWeapon = false;

    [Header("Dash Cancel Windows")]
    [SerializeField] private float dashCancelOpensAtNormalized = 0.35f; // 0..1

    private PlayerContext ctx;

    private PlayerState current;
    private PlayerIdleState idle;
    private PlayerMoveState move;
    private PlayerDashState dash;

    private float nextDashAllowedTime;

    public float DashDistance => dashDistance;
    public float DashDuration => dashDuration;
    public float DashCooldown => dashCooldown;

    public bool AllowCancelDashWithPrimary => allowCancelDashWithPrimary;
    public bool AllowCancelDashWithSecondary => allowCancelDashWithSecondary;
    public bool AllowCancelDashWithSwitchWeapon => allowCancelDashWithSwitchWeapon;
    public float DashCancelOpensAtNormalized => dashCancelOpensAtNormalized;

    public bool CanDash => Time.time >= nextDashAllowedTime;

    public void Initialize(PlayerContext context)
    {
        ctx = context;

        idle = new PlayerIdleState(this, ctx);
        move = new PlayerMoveState(this, ctx);
        dash = new PlayerDashState(this, ctx);

        SetState(idle);
    }

    private void Update()
    {
        if (ctx == null) return;

        // Combat tick (solo si no está bloqueado por el estado)
        ctx.Combat.TickCombat(ctx.Input);

        current?.Tick();
    }

    private void FixedUpdate()
    {
        if (ctx == null) return;
        current?.FixedTick();
    }

    public void SetState(PlayerState next)
    {
        if (next == null) return;

        current?.Exit();
        current = next;
        current.Enter();
    }

    // Helpers de transición
    public void GoIdle() => SetState(idle);
    public void GoMove() => SetState(move);

    public void TryDash(Vector2 dashDir)
    {
        if (!CanDash) return;

        nextDashAllowedTime = Time.time + dashCooldown;
        dash.SetDashDirection(dashDir);
        SetState(dash);
    }
}