using UnityEngine;

public class PlayerContext
{
    public Transform Transform { get; }
    public PlayerInputReader Input { get; }
    public PlayerMovement Movement { get; }
    public PlayerCombatController Combat { get; }
    public PlayerAim Aim { get; }
    public PlayerVisualController Visual { get; }
    public PlayerHealth Health { get; }
    public DashVfxController DashVfx { get; }
    public PlayerDashController DashController { get; }
    public PlayerActionRecorder ActionRecorder { get; }

    public PlayerContext(
        Transform transform,
        PlayerInputReader input,
        PlayerMovement movement,
        PlayerCombatController combat,
        PlayerAim aim,
        PlayerVisualController visual,
        PlayerHealth health,
        DashVfxController dashVfx,
        PlayerDashController dashController,
        PlayerActionRecorder actionRecorder)
    {
        Transform = transform;
        Input = input;
        Movement = movement;
        Combat = combat;
        Aim = aim;
        Visual = visual;
        Health = health;
        DashVfx = dashVfx;
        DashController = dashController;
        ActionRecorder = actionRecorder;
    }
}