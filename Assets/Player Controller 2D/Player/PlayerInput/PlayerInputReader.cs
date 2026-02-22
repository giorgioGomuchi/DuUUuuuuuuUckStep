using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    public event Action<Vector2> MoveEvent;
    public event Action<Vector2> AimEvent;
    public event Action FirePrimaryEvent;
    public event Action FireSecondaryEvent;
    public event Action SwitchWeaponEvent;

    public Vector2 MoveInput { get; private set; }

    private InputSystem_Actions input;

    private void Awake()
    {
        input = new InputSystem_Actions();
        input.Player.SetCallbacks(this);
    }

    private void OnEnable()
    {
        input.Player.Enable();
    }

    private void OnDisable()
    {
        input.Player.Disable();
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        MoveInput = context.ReadValue<Vector2>();
        MoveEvent?.Invoke(MoveInput);

#if UNITY_EDITOR
#endif
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        Vector2 aim = context.ReadValue<Vector2>();
        AimEvent?.Invoke(aim);

#if UNITY_EDITOR
#endif
    }

    public void OnFirePrimary(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            Debug.Log("Primary Fire");
            FirePrimaryEvent?.Invoke();
        }
    }

    public void OnFireSecondary(InputAction.CallbackContext context)
    {
        if (context.performed)
            FireSecondaryEvent?.Invoke();
    }

    public void OnSwitchWeapon(InputAction.CallbackContext context)
    {
        if (context.performed)
            SwitchWeaponEvent?.Invoke();
    }

    // Métodos obligatorios del interface aunque no los uses:
    public void OnAttack(InputAction.CallbackContext context) { }
    public void OnInteract(InputAction.CallbackContext context) { }
    public void OnCrouch(InputAction.CallbackContext context) { }
    public void OnJump(InputAction.CallbackContext context) { }
    public void OnPrevious(InputAction.CallbackContext context) { }
    public void OnNext(InputAction.CallbackContext context) { }
    public void OnSprint(InputAction.CallbackContext context) { }
}
