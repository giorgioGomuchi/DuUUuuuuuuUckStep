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

    public void SetBeam(
        Vector3 start,
        Vector3 end,
        int segments,
        float waveAmplitude,
        float waveFrequency,
        float waveScrollSpeed,
        float time)
    {
        if (lineRenderer == null)
            return;

        segments = Mathf.Max(2, segments);
        lineRenderer.positionCount = segments;

        Vector3 direction = end - start;
        float length = direction.magnitude;

        if (length <= 0.0001f)
        {
            for (int i = 0; i < segments; i++)
                lineRenderer.SetPosition(i, start);

            return;
        }

        Vector3 dir = direction / length;
        Vector3 perpendicular = new Vector3(-dir.y, dir.x, 0f);

        for (int i = 0; i < segments; i++)
        {
            float t = i / (float)(segments - 1);
            Vector3 basePos = Vector3.Lerp(start, end, t);

            // Less distortion at beam start/end, more in the middle.
            float envelope = Mathf.Sin(t * Mathf.PI);

            float wave =
                Mathf.Sin((t * waveFrequency) + (time * waveScrollSpeed)) *
                waveAmplitude *
                envelope;

            Vector3 finalPos = basePos + perpendicular * wave;
            lineRenderer.SetPosition(i, finalPos);
        }
    }
}