using UnityEngine;

public sealed class PlayerContext
{
    public readonly Transform Transform;
    public readonly PlayerInputReader Input;
    public readonly PlayerMovement Movement;
    public readonly PlayerCombatController Combat;
    public readonly PlayerAim Aim;
    public readonly PlayerVisualController Visual;
    public readonly PlayerHealth Health; // opcional para invulnerabilidad
    public readonly DashVfxController DashVfx;

    public PlayerContext(
        Transform transform,
        PlayerInputReader input,
        PlayerMovement movement,
        PlayerCombatController combat,
        PlayerAim aim,
        PlayerVisualController visual,
        PlayerHealth health,
        DashVfxController dashVfx)
    {
        Transform = transform;
        Input = input;
        Movement = movement;
        Combat = combat;
        Aim = aim;
        Visual = visual;
        Health = health;
        DashVfx = dashVfx;
    }
}