using UnityEngine;

public class PlayerRefs : MonoBehaviour
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

    private void Reset()
    {
        input = GetComponentInChildren<PlayerInputReader>();
        stateMachine = GetComponentInChildren<PlayerStateMachine>();
        movement = GetComponentInChildren<PlayerMovement>();
        combat = GetComponentInChildren<PlayerCombatController>();
        aim = GetComponentInChildren<PlayerAim>();
        visual = GetComponentInChildren<PlayerVisualController>();
        anim = GetComponentInChildren<PlayerAnimationController>();
    }
}