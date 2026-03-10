using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class BeamView : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();

        SetVisible(false);
    }

    public void SetVisible(bool visible)
    {
        if (lineRenderer != null)
            lineRenderer.enabled = visible;
    }

    public void SetWidths(float startWidth, float endWidth)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.startWidth = startWidth;
        lineRenderer.endWidth = endWidth;
    }

    public void SetBeam(Vector3 start, Vector3 end)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);
    }
}