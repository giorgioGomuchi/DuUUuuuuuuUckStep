using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class HitscanTracerVFX : MonoBehaviour
{
    [SerializeField] private LineRenderer lineRenderer;

    private float hideTime;

    private void Awake()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
    }

    private void Update()
    {
        if (Time.time >= hideTime)
            Destroy(gameObject);
    }

    public void Play(Vector3 start, Vector3 end, float duration, float widthMultiplier = 1f)
    {
        if (lineRenderer == null)
            return;

        lineRenderer.positionCount = 2;
        lineRenderer.SetPosition(0, start);
        lineRenderer.SetPosition(1, end);

        lineRenderer.startWidth *= widthMultiplier;
        lineRenderer.endWidth *= widthMultiplier;

        hideTime = Time.time + Mathf.Max(0.01f, duration);
    }
}