using UnityEngine;

public class BoomerangProjectileVisuals : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject returnWindowVfxRoot;
    [SerializeField] private GameObject reflectHoldVfxRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private TrailRenderer defaultTrail;
    [SerializeField] private TrailRenderer orbitTrail;

    private Color baseColor = Color.white;
    private Color returningColor = Color.white;
    private Color reflectableColor = Color.yellow;
    private Color orbitStartFlashColor = Color.cyan;

    private float reflectableFlashEndTime;
    private float orbitFlashEndTime;

    private float pulseStartTime;
    private float pulseEndTime;
    private float pulseScaleMultiplier = 1.35f;

    private Vector3 baseScale = Vector3.one;
    private BoomerangProjectileMotorState currentState = BoomerangProjectileMotorState.None;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);

        if (defaultTrail == null)
            defaultTrail = GetComponentInChildren<TrailRenderer>(true);

        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        baseScale = transform.localScale;

        SetReturnWindowActive(false);
        SetReflectHoldActive(false);
        SetTrailMode(false);
    }

    private void Update()
    {
        UpdateColor();
        UpdatePulse();
    }

    public void ApplyConfig(BoomerangProjectileConfig config)
    {
        if (config == null)
            return;

        returningColor = config.returningColor;
        reflectableColor = config.reflectableColor;
        orbitStartFlashColor = config.orbitStartFlashColor;
        pulseScaleMultiplier = Mathf.Max(1f, config.orbitStartPulseScaleMultiplier);
    }

    public void ResetVisuals()
    {
        currentState = BoomerangProjectileMotorState.None;
        reflectableFlashEndTime = 0f;
        orbitFlashEndTime = 0f;
        pulseStartTime = 0f;
        pulseEndTime = 0f;

        transform.localScale = baseScale;

        if (spriteRenderer != null)
            spriteRenderer.color = baseColor;

        SetReturnWindowActive(false);
        SetReflectHoldActive(false);
        SetTrailMode(false);
    }

    public void SetMotorState(BoomerangProjectileMotorState state)
    {
        currentState = state;

        SetTrailMode(state == BoomerangProjectileMotorState.Orbiting);
        SetReflectHoldActive(state == BoomerangProjectileMotorState.ReflectHold);
    }

    public void SetReturnWindowActive(bool active)
    {
        if (returnWindowVfxRoot != null)
            returnWindowVfxRoot.SetActive(active);
    }

    public void TriggerReflectableFlash(float duration)
    {
        reflectableFlashEndTime = Time.time + Mathf.Max(0.01f, duration);
    }

    public void TriggerOrbitStartFeedback(float flashDuration, float pulseDuration)
    {
        orbitFlashEndTime = Time.time + Mathf.Max(0.01f, flashDuration);
        pulseStartTime = Time.time;
        pulseEndTime = Time.time + Mathf.Max(0.01f, pulseDuration);
    }

    private void UpdateColor()
    {
        if (spriteRenderer == null)
            return;

        if (Time.time < orbitFlashEndTime)
        {
            spriteRenderer.color = orbitStartFlashColor;
            return;
        }

        switch (currentState)
        {
            case BoomerangProjectileMotorState.Returning:
            case BoomerangProjectileMotorState.Orbiting:
                spriteRenderer.color = returningColor;
                break;

            case BoomerangProjectileMotorState.ReflectHold:
                spriteRenderer.color = Time.time < reflectableFlashEndTime
                    ? reflectableColor
                    : returningColor;
                break;

            default:
                spriteRenderer.color = baseColor;
                break;
        }
    }

    private void UpdatePulse()
    {
        if (Time.time >= pulseEndTime)
        {
            transform.localScale = baseScale;
            return;
        }

        float t = Mathf.InverseLerp(pulseStartTime, pulseEndTime, Time.time);
        float scale = Mathf.Lerp(pulseScaleMultiplier, 1f, t);
        transform.localScale = baseScale * scale;
    }

    private void SetReflectHoldActive(bool active)
    {
        if (reflectHoldVfxRoot != null)
            reflectHoldVfxRoot.SetActive(active);
    }

    private void SetTrailMode(bool orbitMode)
    {
        if (defaultTrail != null)
        {
            defaultTrail.emitting = !orbitMode;
            if (!orbitMode)
                defaultTrail.Clear();
        }

        if (orbitTrail != null)
        {
            orbitTrail.emitting = orbitMode;
            if (orbitMode)
                orbitTrail.Clear();
        }
    }
}