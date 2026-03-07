using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO config;

    [Header("Core")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerStateMachine stateMachine;

    [Header("Movement")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerDashController dashController;

    [Header("Combat")]
    [SerializeField] private PlayerCombatController combat;

    [Header("Combo")]
    [SerializeField] private PlayerActionRecorder actionRecorder;

    [Header("Visual")]
    [SerializeField] private PlayerAim aim;
    [SerializeField] private PlayerVisualController visual;
    [SerializeField] private PlayerAnimationController anim;

    [Header("Dash VFX")]
    [SerializeField] private DashVfxController dashVfx;

    [Header("Health")]
    [SerializeField] private PlayerHealth health;

    private PlayerContext ctx;

    private void Awake()
    {
        ResolveReferences();
        ApplyConfig();
        BuildContext();
        WireEvents();

        stateMachine.Initialize(ctx);
    }

    private void ResolveReferences()
    {
        if (input == null) input = GetComponentInChildren<PlayerInputReader>();
        if (stateMachine == null) stateMachine = GetComponentInChildren<PlayerStateMachine>();

        if (movement == null) movement = GetComponentInChildren<PlayerMovement>();
        if (dashController == null) dashController = GetComponentInChildren<PlayerDashController>();

        if (combat == null) combat = GetComponentInChildren<PlayerCombatController>();
        if (actionRecorder == null) actionRecorder = GetComponentInChildren<PlayerActionRecorder>();

        if (aim == null) aim = GetComponentInChildren<PlayerAim>();
        if (visual == null) visual = GetComponentInChildren<PlayerVisualController>();
        if (anim == null) anim = GetComponentInChildren<PlayerAnimationController>();
        if (dashVfx == null) dashVfx = GetComponentInChildren<DashVfxController>();

        if (health == null) health = GetComponent<PlayerHealth>();
    }

    private void ApplyConfig()
    {
        movement?.SetConfig(config);
        dashController?.SetConfig(config);
        stateMachine?.SetConfig(config);
        health?.SetConfig(config);
    }

    private void BuildContext()
    {
        ctx = new PlayerContext(
            transform,
            input,
            movement,
            combat,
            aim,
            visual,
            health,
            dashVfx,
            dashController,
            actionRecorder
        );
    }

    private void WireEvents()
    {
        if (input != null && aim != null)
            input.OnAimScreen += aim.SetAim;

        if (aim != null && visual != null)
            aim.OnAimChanged += visual.SetAim;

        if (aim != null && combat != null)
            aim.OnAimChanged += combat.SetAim;
    }
}