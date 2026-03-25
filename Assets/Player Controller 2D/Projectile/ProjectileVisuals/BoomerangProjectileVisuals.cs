using UnityEngine;

public class BoomerangProjectileVisuals : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject returnWindowVfxRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer defaultTrail;
    [SerializeField] private TrailRenderer orbitTrail;

    [Header("Colors")]
    [SerializeField] private Color returningColor = Color.white;

    private Color baseColor = Color.white;
    private Color reflectableColor = Color.yellow;
    private float reflectableFlashEndTime;

    private Color orbitStartFlashColor = new Color(0.3f, 1f, 1f, 1f);
    private float orbitFlashEndTime;

    private float orbitPulseStartTime;
    private float orbitPulseEndTime;
    private float orbitStartPulseScaleMultiplier = 1.35f;
    private Vector3 baseVisualScale = Vector3.one;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>();

        if (defaultTrail == null)
            defaultTrail = GetComponentInChildren<TrailRenderer>();

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        baseVisualScale = transform.localScale;

        SetReturnWindowActive(false);
        SetOrbitTrailActive(false);
        SetDefaultTrailActive(true);
    }

    private void Update()
    {
        UpdatePulse();
        UpdateColor();
    }

    public void ConfigureReflectableFeedback(Color color, float flashDuration)
    {
        reflectableColor = color;
    }

    public void ConfigureOrbitStartFeedback(Color flashColor, float pulseScaleMultiplier)
    {
        orbitStartFlashColor = flashColor;
        orbitStartPulseScaleMultiplier = Mathf.Max(1f, pulseScaleMultiplier);
    }

    public void SetReturningColor(Color color)
    {
        returningColor = color;
    }

    public void SetReturnWindowActive(bool active)
    {
        if (returnWindowVfxRoot != null)
            returnWindowVfxRoot.SetActive(active);
    }

    public void SetState(BoomerangFlightState state)
    {
        bool orbitActive = state == BoomerangFlightState.OrbitingExpanding;
        SetOrbitTrailActive(orbitActive);

        // Trail normal activo fuera de órbita
        SetDefaultTrailActive(!orbitActive);
    }

    public void TriggerReflectableFlash(float duration)
    {
        reflectableFlashEndTime = Time.time + Mathf.Max(0.01f, duration);
    }

    public void TriggerOrbitStartFeedback(float flashDuration, float pulseDuration)
    {
        orbitFlashEndTime = Time.time + Mathf.Max(0.01f, flashDuration);
        orbitPulseStartTime = Time.time;
        orbitPulseEndTime = Time.time + Mathf.Max(0.01f, pulseDuration);
    }

    public void ApplyVisualState(BoomerangFlightState state)
    {
        if (spriteRenderer == null)
            return;

        if (Time.time < orbitFlashEndTime)
        {
            spriteRenderer.color = orbitStartFlashColor;
            return;
        }

        if (state == BoomerangFlightState.ReflectableReturning)
        {
            spriteRenderer.color = Time.time < reflectableFlashEndTime ? reflectableColor : returningColor;
            return;
        }

        if (state == BoomerangFlightState.ReturningCurved ||
            state == BoomerangFlightState.OrbitingExpanding)
        {
            spriteRenderer.color = returningColor;
            return;
        }

        spriteRenderer.color = baseColor;
    }

    public void ResetVisuals()
    {
        transform.localScale = baseVisualScale;
        orbitFlashEndTime = 0f;
        reflectableFlashEndTime = 0f;

        if (spriteRenderer != null)
            spriteRenderer.color = baseColor;

        SetReturnWindowActive(false);
        SetOrbitTrailActive(false);
        SetDefaultTrailActive(true);
    }

    private void UpdateColor()
    {
        BoomerangProjectile2D projectile = GetComponent<BoomerangProjectile2D>();
        if (projectile == null)
            return;

        ApplyVisualState(projectile.FlightState);
    }

    private void UpdatePulse()
    {
        if (Time.time >= orbitPulseEndTime)
        {
            transform.localScale = baseVisualScale;
            return;
        }

        float t = Mathf.InverseLerp(orbitPulseStartTime, orbitPulseEndTime, Time.time);
        float scale = Mathf.Lerp(orbitStartPulseScaleMultiplier, 1f, t);
        transform.localScale = baseVisualScale * scale;
    }

    private void SetDefaultTrailActive(bool active)
    {
        if (defaultTrail == null)
            return;

        defaultTrail.emitting = active;
        if (active)
            defaultTrail.Clear();
    }

    private void SetOrbitTrailActive(bool active)
    {
        if (orbitTrail == null)
            return;

        orbitTrail.emitting = active;
        if (active)
            orbitTrail.Clear();
    }
}