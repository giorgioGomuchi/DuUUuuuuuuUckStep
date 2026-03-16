using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInputReader : MonoBehaviour, InputSystem_Actions.IPlayerActions
{
    public event Action<Vector2> OnAimScreen;
    public event Action<Vector2> OnAimStick;

    [Header("Debug")]
    [SerializeField] private bool debugFireInputLogs = false;

    [Header("Aim")]
    [SerializeField] private float stickDeadzone = 0.2f;

    [Header("Fallback Defaults")]
    [SerializeField] private FireInputMode defaultPrimaryFireMode = FireInputMode.SinglePress;
    [SerializeField] private FireInputMode defaultSecondaryFireMode = FireInputMode.SinglePress;

    [Header("Repeat")]
    [SerializeField] private float primaryHoldRepeatInterval = 0.12f;
    [SerializeField] private float secondaryHoldRepeatInterval = 0.12f;

    [Header("Trigger Gates")]
    [SerializeField] private AnalogTriggerGate primaryTriggerGate = new();
    [SerializeField] private AnalogTriggerGate secondaryTriggerGate = new();

    public Vector2 Move { get; private set; }
    public Vector2 AimScreen { get; private set; }
    public Vector2 AimStickValue { get; private set; }

    public bool FirePrimaryHeld => primaryTriggerGate.IsHeld;
    public bool FireSecondaryHeld => secondaryTriggerGate.IsHeld;

    public bool DashPressed { get; private set; }
    public bool SwitchWeaponPressed { get; private set; }

    public FireInputMode PrimaryFireMode => primaryFireMode;
    public FireInputMode SecondaryFireMode => secondaryFireMode;

    private bool firePrimaryPressed;
    private bool fireSecondaryPressed;

    private InputSystem_Actions input;

    private FireInputMode primaryFireMode;
    private FireInputMode secondaryFireMode;

    private bool hasForcedPrimaryMode;
    private bool hasForcedSecondaryMode;

    private float primaryHeldTime;
    private float secondaryHeldTime;
    private float nextPrimaryRepeatTime;
    private float nextSecondaryRepeatTime;

    private void Awake()
    {
        input = new InputSystem_Actions();
        input.Player.SetCallbacks(this);

        primaryFireMode = defaultPrimaryFireMode;
        secondaryFireMode = defaultSecondaryFireMode;
    }

    private void OnEnable()
    {
        input.Player.Enable();
        ResetInputState();
    }

    private void OnDisable()
    {
        input.Player.Disable();
    }

    private void Update()
    {
        firePrimaryPressed = false;
        fireSecondaryPressed = false;

        UpdateHoldRepeat(
            primaryTriggerGate,
            primaryFireMode,
            primaryHoldRepeatInterval,
            ref primaryHeldTime,
            ref nextPrimaryRepeatTime,
            ref firePrimaryPressed);

        UpdateHoldRepeat(
            secondaryTriggerGate,
            secondaryFireMode,
            secondaryHoldRepeatInterval,
            ref secondaryHeldTime,
            ref nextSecondaryRepeatTime,
            ref fireSecondaryPressed);

        if (debugFireInputLogs && firePrimaryPressed)
        {
            Debug.Log(
                $"[InputReader] Primary fire request | mode={primaryFireMode} | held={primaryTriggerGate.IsHeld}",
                this);
        }

        if (debugFireInputLogs && fireSecondaryPressed)
        {
            Debug.Log(
                $"[InputReader] Secondary fire request | mode={secondaryFireMode} | held={secondaryTriggerGate.IsHeld}",
                this);
        }

        primaryTriggerGate.ClearFrameFlags();
        secondaryTriggerGate.ClearFrameFlags();
    }

    private void UpdateHoldRepeat(
        AnalogTriggerGate gate,
        FireInputMode mode,
        float repeatInterval,
        ref float heldTime,
        ref float nextRepeatTime,
        ref bool pressedFlag)
    {
        if (gate.PressedThisFrame)
        {
            pressedFlag = true;
            heldTime = 0f;
            nextRepeatTime = Mathf.Max(0.01f, repeatInterval);
            return;
        }

        if (!gate.IsHeld)
        {
            heldTime = 0f;
            nextRepeatTime = Mathf.Max(0.01f, repeatInterval);
            return;
        }

        heldTime += Time.deltaTime;

        if (mode != FireInputMode.HoldRepeat)
            return;

        if (heldTime >= nextRepeatTime)
        {
            pressedFlag = true;
            nextRepeatTime += Mathf.Max(0.01f, repeatInterval);
        }
    }

    public void ApplyWeaponResolvedFireModes(FireInputMode primaryMode, FireInputMode secondaryMode, bool forceRelease = false)
    {
        if (!hasForcedPrimaryMode)
            primaryFireMode = primaryMode;

        if (!hasForcedSecondaryMode)
            secondaryFireMode = secondaryMode;

        if (forceRelease)
            ForceReleaseFireInputs();
    }

    public void ForcePrimaryFireMode(FireInputMode mode, bool forceRelease = false)
    {
        hasForcedPrimaryMode = true;
        primaryFireMode = mode;

        if (forceRelease)
        {
            primaryTriggerGate.ForceRelease();
            firePrimaryPressed = false;
            primaryHeldTime = 0f;
            nextPrimaryRepeatTime = Mathf.Max(0.01f, primaryHoldRepeatInterval);
        }
    }

    public void ForceSecondaryFireMode(FireInputMode mode, bool forceRelease = false)
    {
        hasForcedSecondaryMode = true;
        secondaryFireMode = mode;

        if (forceRelease)
        {
            secondaryTriggerGate.ForceRelease();
            fireSecondaryPressed = false;
            secondaryHeldTime = 0f;
            nextSecondaryRepeatTime = Mathf.Max(0.01f, secondaryHoldRepeatInterval);
        }
    }

    public void ClearForcedFireModes(bool forceRelease = false)
    {
        hasForcedPrimaryMode = false;
        hasForcedSecondaryMode = false;

        primaryFireMode = defaultPrimaryFireMode;
        secondaryFireMode = defaultSecondaryFireMode;

        if (forceRelease)
            ForceReleaseFireInputs();
    }

    public void BeginSequenceInputOverride(bool forcePrimarySinglePress, bool forceSecondarySinglePress)
    {
        ForceReleaseFireInputs();
        ClearBufferedInputs();

        if (forcePrimarySinglePress)
            ForcePrimaryFireMode(FireInputMode.SinglePress, forceRelease: true);

        if (forceSecondarySinglePress)
            ForceSecondaryFireMode(FireInputMode.SinglePress, forceRelease: true);
    }

    public void EndSequenceInputOverride()
    {
        ClearForcedFireModes(forceRelease: true);
        ClearBufferedInputs();
    }

    public void ForceReleaseFireInputs()
    {
        primaryTriggerGate.ForceRelease();
        secondaryTriggerGate.ForceRelease();

        firePrimaryPressed = false;
        fireSecondaryPressed = false;

        primaryHeldTime = 0f;
        secondaryHeldTime = 0f;

        nextPrimaryRepeatTime = Mathf.Max(0.01f, primaryHoldRepeatInterval);
        nextSecondaryRepeatTime = Mathf.Max(0.01f, secondaryHoldRepeatInterval);
    }

    private void ResetInputState()
    {
        Move = Vector2.zero;
        AimScreen = Vector2.zero;
        AimStickValue = Vector2.zero;

        firePrimaryPressed = false;
        fireSecondaryPressed = false;
        DashPressed = false;
        SwitchWeaponPressed = false;

        primaryHeldTime = 0f;
        secondaryHeldTime = 0f;

        nextPrimaryRepeatTime = Mathf.Max(0.01f, primaryHoldRepeatInterval);
        nextSecondaryRepeatTime = Mathf.Max(0.01f, secondaryHoldRepeatInterval);

        primaryTriggerGate.ForceRelease();
        secondaryTriggerGate.ForceRelease();

        hasForcedPrimaryMode = false;
        hasForcedSecondaryMode = false;
        primaryFireMode = defaultPrimaryFireMode;
        secondaryFireMode = defaultSecondaryFireMode;
    }

    public bool ConsumePrimaryFireRequest()
    {
        if (!firePrimaryPressed)
            return false;

        firePrimaryPressed = false;
        return true;
    }

    public bool ConsumeSecondaryFireRequest()
    {
        if (!fireSecondaryPressed)
            return false;

        fireSecondaryPressed = false;
        return true;
    }

    public bool ConsumeDashPressed()
    {
        if (!DashPressed)
            return false;

        DashPressed = false;
        return true;
    }

    public bool ConsumeSwitchWeaponPressed()
    {
        if (!SwitchWeaponPressed)
            return false;

        SwitchWeaponPressed = false;
        return true;
    }

    public void ClearBufferedInputs()
    {
        firePrimaryPressed = false;
        fireSecondaryPressed = false;
        DashPressed = false;
        SwitchWeaponPressed = false;
    }

    public void OnMove(InputAction.CallbackContext context)
    {
        Move = context.ReadValue<Vector2>();
    }

    public void OnAim(InputAction.CallbackContext context)
    {
        Vector2 value = context.ReadValue<Vector2>();
        InputDevice device = context.control?.device;

        if (device is Mouse || context.control?.path.Contains("position") == true)
        {
            AimScreen = value;
            OnAimScreen?.Invoke(AimScreen);
            return;
        }

        if (device is Gamepad || device is Joystick)
        {
            if (value.sqrMagnitude >= stickDeadzone * stickDeadzone)
            {
                AimStickValue = value;
                OnAimStick?.Invoke(AimStickValue);
            }
            else
            {
                AimStickValue = Vector2.zero;
            }
        }
    }

    public void OnFirePrimary(InputAction.CallbackContext context)
    {
        float rawValue = context.ReadValue<float>();
        primaryTriggerGate.UpdateValue(rawValue);

        if (debugFireInputLogs)
        {
            Debug.Log(
                $"[InputReader] Primary raw={rawValue:F3} held={primaryTriggerGate.IsHeld} " +
                $"pressedFrame={primaryTriggerGate.PressedThisFrame} releasedFrame={primaryTriggerGate.ReleasedThisFrame} " +
                $"mode={primaryFireMode}",
                this);
        }
    }

    public void OnFireSecondary(InputAction.CallbackContext context)
    {
        float rawValue = context.ReadValue<float>();
        secondaryTriggerGate.UpdateValue(rawValue);

        if (debugFireInputLogs)
        {
            Debug.Log(
                $"[InputReader] Secondary raw={rawValue:F3} held={secondaryTriggerGate.IsHeld} " +
                $"pressedFrame={secondaryTriggerGate.PressedThisFrame} releasedFrame={secondaryTriggerGate.ReleasedThisFrame} " +
                $"mode={secondaryFireMode}",
                this);
        }
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