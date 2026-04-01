using UnityEngine;

public class PlayerReferences : MonoBehaviour
{
    [Header("Core")]
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerStateMachine stateMachine;
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerDashController dashController;
    [SerializeField] private PlayerCombatController combat;
    [SerializeField] private PlayerActionRecorder actionRecorder;
    [SerializeField] private PlayerAim aim;
    [SerializeField] private PlayerVisualController visual;
    [SerializeField] private PlayerAnimationController anim;
    [SerializeField] private DashVfxController dashVfx;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private WeaponSlotsController weaponSlots;
    [SerializeField] private WeaponOverrideController weaponOverride;
    [SerializeField] private WeaponAimGuideController weaponAimGuide;
    [SerializeField] private WeaponSequenceActorAdapter weaponSequenceActorAdapter;
    [SerializeField] private WeaponSequenceControllerV2 weaponSequenceControllerV2;

    [SerializeField] private ShotgunSequenceController shotgunSequenceController;

    public PlayerInputReader Input => input;
    public PlayerStateMachine StateMachine => stateMachine;
    public PlayerMovement Movement => movement;
    public PlayerDashController DashController => dashController;
    public PlayerCombatController Combat => combat;
    public PlayerActionRecorder ActionRecorder => actionRecorder;
    public PlayerAim Aim => aim;
    public PlayerVisualController Visual => visual;
    public PlayerAnimationController Anim => anim;
    public DashVfxController DashVfx => dashVfx;
    public PlayerHealth Health => health;
    public WeaponSlotsController WeaponSlots => weaponSlots;
    public WeaponOverrideController WeaponOverride => weaponOverride;
    public WeaponAimGuideController WeaponAimGuide => weaponAimGuide;
    public WeaponSequenceActorAdapter WeaponSequenceActorAdapter => weaponSequenceActorAdapter;
    public WeaponSequenceControllerV2 WeaponSequenceControllerV2 => weaponSequenceControllerV2;

    public ShotgunSequenceController ShotgunSequenceController => shotgunSequenceController;


#if UNITY_EDITOR
    private void OnValidate()
    {
        if (input == null) input = GetComponentInChildren<PlayerInputReader>(true);
        if (stateMachine == null) stateMachine = GetComponentInChildren<PlayerStateMachine>(true);
        if (movement == null) movement = GetComponentInChildren<PlayerMovement>(true);
        if (dashController == null) dashController = GetComponentInChildren<PlayerDashController>(true);
        if (combat == null) combat = GetComponentInChildren<PlayerCombatController>(true);
        if (actionRecorder == null) actionRecorder = GetComponentInChildren<PlayerActionRecorder>(true);
        if (aim == null) aim = GetComponentInChildren<PlayerAim>(true);
        if (visual == null) visual = GetComponentInChildren<PlayerVisualController>(true);
        if (anim == null) anim = GetComponentInChildren<PlayerAnimationController>(true);
        if (dashVfx == null) dashVfx = GetComponentInChildren<DashVfxController>(true);
        if (health == null) health = GetComponentInChildren<PlayerHealth>(true);
        if (weaponSlots == null) weaponSlots = GetComponentInChildren<WeaponSlotsController>(true);
        if (weaponOverride == null) weaponOverride = GetComponentInChildren<WeaponOverrideController>(true);
        if (weaponAimGuide == null) weaponAimGuide = GetComponentInChildren<WeaponAimGuideController>(true);
        if (weaponSequenceActorAdapter == null) weaponSequenceActorAdapter = GetComponentInChildren<WeaponSequenceActorAdapter>(true);
        if (weaponSequenceControllerV2 == null) weaponSequenceControllerV2 = GetComponentInChildren<WeaponSequenceControllerV2>(true);

        if (shotgunSequenceController == null) shotgunSequenceController = GetComponentInChildren<ShotgunSequenceController>(true);

    }
#endif
}