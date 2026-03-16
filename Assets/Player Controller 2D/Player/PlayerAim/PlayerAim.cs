using System;
using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    public enum AimSource
    {
        None,
        Mouse,
        Gamepad
    }

    public event Action<Vector2> OnAimChanged;

    [Header("General")]
    [SerializeField] private float stickDeadzone = 0.2f;

    [Header("Gamepad Aim Radius")]
    [SerializeField] private float gamepadMinAimRadius = 2.0f;
    [SerializeField] private float gamepadMaxAimRadius = 4.0f;

    public Vector2 CurrentAim { get; private set; } = Vector2.right;
    public Vector2 CursorWorldPosition { get; private set; }
    public Camera MainCamera => mainCamera;
    public AimSource CurrentSource { get; private set; } = AimSource.None;

    private Camera mainCamera;

    private Vector2 lastStickInput;
    private float currentGamepadRadius;

    private void Awake()
    {
        mainCamera = Camera.main;
        currentGamepadRadius = gamepadMinAimRadius;
        RebuildGamepadCursorPosition();
    }

    private void LateUpdate()
    {
        // Con gamepad, la mira debe recalcularse siempre respecto al player,
        // incluso si no entra input nuevo este frame.
        if (CurrentSource == AimSource.Gamepad)
        {
            RebuildGamepadCursorPosition();
        }
    }

    public void SetAim(Vector2 screenPosition)
    {
        SetAimFromScreen(screenPosition);
    }

    public void SetAimFromScreen(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return;

        CurrentSource = AimSource.Mouse;

        Vector3 world = mainCamera.ScreenToWorldPoint(screenPosition);
        CursorWorldPosition = new Vector2(world.x, world.y);

        Vector2 dir = CursorWorldPosition - (Vector2)transform.position;
        if (dir.sqrMagnitude <= 0.0001f)
            return;

        CurrentAim = dir.normalized;
        OnAimChanged?.Invoke(CurrentAim);

#if UNITY_EDITOR
        Debug.DrawLine(transform.position, CursorWorldPosition, Color.green);
#endif
    }

    public void SetAimFromStick(Vector2 stickInput)
    {
        CurrentSource = AimSource.Gamepad;
        lastStickInput = stickInput;

        float magnitude = stickInput.magnitude;

        // Si el stick está dentro de deadzone, mantenemos la última dirección válida
        // y colocamos la mira en el radio mínimo.
        if (magnitude < stickDeadzone)
        {
            currentGamepadRadius = gamepadMinAimRadius;
            RebuildGamepadCursorPosition();
            return;
        }

        CurrentAim = stickInput.normalized;

        float normalizedMagnitude = Mathf.InverseLerp(stickDeadzone, 1f, Mathf.Clamp01(magnitude));
        currentGamepadRadius = Mathf.Lerp(gamepadMinAimRadius, gamepadMaxAimRadius, normalizedMagnitude);

        RebuildGamepadCursorPosition();
        OnAimChanged?.Invoke(CurrentAim);

#if UNITY_EDITOR
        Debug.DrawLine(transform.position, CurrentAim, Color.green);
#endif
    }

    private void RebuildGamepadCursorPosition()
    {
        CursorWorldPosition = (Vector2)transform.position + CurrentAim * currentGamepadRadius;
    }

    public Vector2 GetAimWorldPosition()
    {
        return CursorWorldPosition;
    }

    public float GetCurrentGamepadRadius()
    {
        return currentGamepadRadius;
    }

    public void SetGamepadAimRadius(float minRadius, float maxRadius)
    {
        gamepadMinAimRadius = Mathf.Max(0f, minRadius);
        gamepadMaxAimRadius = Mathf.Max(gamepadMinAimRadius, maxRadius);
        currentGamepadRadius = Mathf.Clamp(currentGamepadRadius, gamepadMinAimRadius, gamepadMaxAimRadius);
        RebuildGamepadCursorPosition();
    }
}