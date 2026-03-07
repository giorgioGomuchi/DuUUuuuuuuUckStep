using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [Header("Core")]
    public PlayerInputReader input;
    public PlayerStateMachine stateMachine;

    [Header("Movement")]
    public PlayerMovement movement;

    [Header("Combat")]
    public PlayerCombatController combat;

    [Header("Visual")]
    public PlayerAim aim;
    public PlayerVisualController visual; // tu script
    public PlayerAnimationController anim; // opcional

    [Header("DashVfx")]
    public DashVfxController dashVfx;

    [SerializeField] private PlayerHealth health; // si lo tienes en el mismo GO

    private PlayerContext ctx;

    private void Awake()
    {
        input = GetComponentInChildren<PlayerInputReader>();
        stateMachine = GetComponentInChildren<PlayerStateMachine>();
        movement = GetComponentInChildren<PlayerMovement>();
        combat = GetComponentInChildren<PlayerCombatController>();
        aim = GetComponentInChildren<PlayerAim>();
        visual = GetComponentInChildren<PlayerVisualController>();
        anim = GetComponentInChildren<PlayerAnimationController>();
        dashVfx = GetComponentInChildren<DashVfxController>();


        if (health == null) health = GetComponent<PlayerHealth>();

        // Context
        ctx = new PlayerContext(
            transform,
            input,
            movement,
            combat,
            aim,
            visual,
            health,
            dashVfx
        );

        // Cableado VISUAL/AIM (NO gameplay)
        // Input da screen position; Aim lo traduce a dirección mundo y notifica.
        input.OnAimScreen += aim.SetAim;

        aim.OnAimChanged += visual.SetAim;
        aim.OnAimChanged += combat.SetAim;

        // Inicializa StateMachine con context
        stateMachine.Initialize(ctx);
    }
}