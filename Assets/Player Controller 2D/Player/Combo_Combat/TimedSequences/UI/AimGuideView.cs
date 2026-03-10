using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class AimGuideView : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LineRenderer lineRenderer;

    [Header("Visual")]
    [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.45f);
    [SerializeField] private Color flashColor = Color.cyan;
    [SerializeField] private float idleWidth = 0.03f;
    [SerializeField] private float flashWidthMultiplier = 1.75f;
    [SerializeField] private float flashDuration = 0.08f;

    private bool isVisible;
    private float flashEndTime;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        ApplyIdleVisual();
        SetVisible(false);
    }

    private void Update()
    {
        if (!isVisible || lineRenderer == null)
            return;

        if (flashEndTime > 0f && Time.time >= flashEndTime)
        {
            flashEndTime = 0f;
            ApplyIdleVisual();
        }
    }

    public void SetLine(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }

    public void Show()
    {
        SetVisible(true);
        ApplyIdleVisual();
    }

    public void Hide()
    {
        SetVisible(false);
        flashEndTime = 0f;
    }

    public void FlashShot()
    {
        if (lineRenderer == null)
            return;

        SetVisible(true);

        lineRenderer.startColor = flashColor;
        lineRenderer.endColor = flashColor;
        lineRenderer.startWidth = idleWidth * flashWidthMultiplier;
        lineRenderer.endWidth = idleWidth * flashWidthMultiplier;

        flashEndTime = Time.time + Mathf.Max(0.01f, flashDuration);
    }

    private void ApplyIdleVisual()
    {
        if (lineRenderer == null)
            return;

        lineRenderer.startColor = idleColor;
        lineRenderer.endColor = idleColor;
        lineRenderer.startWidth = idleWidth;
        lineRenderer.endWidth = idleWidth;
    }

    private void SetVisible(bool visible)
    {
        isVisible = visible;

        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }
}