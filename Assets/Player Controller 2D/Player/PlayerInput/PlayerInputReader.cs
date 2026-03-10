using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    public event Action<Vector2> OnAimScreen;

    public Vector2 Move { get; private set; }
    public Vector2 AimScreen { get; private set; }

    public bool FirePrimaryHeld { get; private set; }
    public bool FireSecondaryHeld { get; private set; }

    public bool FirePrimaryPressed { get; private set; }
    public bool FireSecondaryPressed { get; private set; }

    public bool DashPressed { get; private set; }
    public bool SwitchWeaponPressed { get; private set; }

    private InputSystem_Actions input;

    private void Awake()
    {
        input = new InputSystem_Actions();
        input.Player.SetCallbacks(this);
    }

    private void OnEnable() => input.Player.Enable();
    private void OnDisable() => input.Player.Disable();

    public bool ConsumeFirePrimaryPressed()
    {
        if (!FirePrimaryPressed) return false;
        FirePrimaryPressed = false;
        return true;
    }

    public bool ConsumeFireSecondaryPressed()
    {
        if (!FireSecondaryPressed) return false;
        FireSecondaryPressed = false;
        return true;
    }

    public bool ConsumeDashPressed()
    {
        if (!DashPressed) return false;
        DashPressed = false;
        return true;
    }

    public bool ConsumeSwitchWeaponPressed()
    {
        if (!SwitchWeaponPressed) return false;
        SwitchWeaponPressed = false;
        return true;
    }

    public void ClearBufferedInputs()
    {
        FirePrimaryPressed = false;
        FireSecondaryPressed = false;
        DashPressed = false;
        SwitchWeaponPressed = false;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        AimScreen = context.ReadValue<Vector2>();
        OnAimScreen?.Invoke(AimScreen);
    }

    public void OnFirePrimary(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            FirePrimaryHeld = true;
            FirePrimaryPressed = true;
        }

        if (context.canceled)
            FirePrimaryHeld = false;
    }

    public void OnFireSecondary(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            FireSecondaryHeld = true;
            FireSecondaryPressed = true;
        }

        if (context.canceled)
            FireSecondaryHeld = false;
    }

    public void OnSwitchWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
            SwitchWeaponPressed = true;
    }

    public void OnJump(InputAction.CallbackContext context)
    {
        if (context.performed)
            DashPressed = true;
    }

    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
    public void OnSprint(InputAction.CallbackContext context) { }
}