using System;
using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    public event Action<Vector2> OnAimChanged;

    public Vector2 CurrentAim { get; private set; }
    public Vector2 CursorWorldPosition { get; private set; }
    public Camera MainCamera => mainCamera;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void SetAim(Vector2 screenPosition)
    {
        if (mainCamera == null)
            return;

        CursorWorldPosition = mainCamera.ScreenToWorldPoint(screenPosition);

        Vector2 dir = (CursorWorldPosition - (Vector2)transform.position).normalized;
        CurrentAim = dir;

        OnAimChanged?.Invoke(CurrentAim);

#if UNITY_EDITOR
        Debug.DrawLine(transform.position, CursorWorldPosition, Color.red);
#endif
    }
}