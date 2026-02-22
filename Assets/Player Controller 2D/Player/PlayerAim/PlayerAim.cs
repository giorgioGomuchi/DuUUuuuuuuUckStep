using System;
using UnityEngine;

public class PlayerAim : MonoBehaviour
{
    public event Action<Vector2> OnAimChanged;

    public Vector2 CurrentAim { get; private set; }

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void SetAim(Vector2 screenPosition)
    {
        if (mainCamera == null) return;

        Vector2 worldMouse = mainCamera.ScreenToWorldPoint(screenPosition);

        Vector2 dir = (worldMouse - (Vector2)transform.position).normalized;

        CurrentAim = dir;

        OnAimChanged?.Invoke(CurrentAim);

#if UNITY_EDITOR
        Debug.DrawLine(transform.position, worldMouse, Color.red);
#endif
    }
}
