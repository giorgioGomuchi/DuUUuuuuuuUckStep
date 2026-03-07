using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO config;

    [Header("References")]
    [SerializeField] private PlayerDashController dashController;

    private PlayerContext ctx;

    private PlayerState current;
    private PlayerIdleState idle;
    private PlayerMoveState move;
    private PlayerDashState dash;

    private float nextDashAllowedTime;

    public PlayerDashController DashController => dashController;

    public bool AllowCancelDashWithPrimary => config != null && config.allowCancelDashWithPrimary;
    public bool AllowCancelDashWithSecondary => config != null && config.allowCancelDashWithSecondary;
    public bool AllowCancelDashWithSwitchWeapon => config != null && config.allowCancelDashWithSwitchWeapon;
    public float DashCancelOpensAtNormalized => config != null ? config.dashCancelOpensAtNormalized : 0.35f;

    public bool CanDash
    {
        get
        {
            float cooldown = config != null ? config.dashCooldown : 0.45f;
            return Time.time >= nextDashAllowedTime;
        }
    }

    public void SetConfig(PlayerConfigSO playerConfig)
    {
        config = playerConfig;
    }

    public void Initialize(PlayerContext context)
    {
        ctx = context;

        if (dashController == null)
            dashController = GetComponentInChildren<PlayerDashController>();

        idle = new PlayerIdleState(this, ctx);
        move = new PlayerMoveState(this, ctx);
        dash = new PlayerDashState(this, ctx);

        SetState(idle);
    }

    private void Update()
    {
        if (ctx == null) return;

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

    public void GoIdle()
    {
        ctx.Combat.SetCombatBlocked(false);
        SetState(idle);
    }

    public void GoMove()
    {
        ctx.Combat.SetCombatBlocked(false);
        SetState(move);
    }

    public void TryDash(Vector2 dashDir)
    {
        if (!CanDash) return;

        float cooldown = config != null ? config.dashCooldown : 0.45f;
        nextDashAllowedTime = Time.time + cooldown;

        dash.SetDashDirection(dashDir);
        SetState(dash);
    }

    public Vector2 LastNonZeroMoveDir { get; private set; } = Vector2.right;

    public void UpdateLastNonZeroMove(Vector2 moveInput)
    {
        if (moveInput.sqrMagnitude > 0.0001f)
            LastNonZeroMoveDir = moveInput.normalized;
    }
}