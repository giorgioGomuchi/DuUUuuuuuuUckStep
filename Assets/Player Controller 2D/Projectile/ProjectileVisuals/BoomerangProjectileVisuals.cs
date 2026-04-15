using UnityEngine;

public class BoomerangProjectileVisuals : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private GameObject returnWindowVfxRoot;
    [SerializeField] private GameObject reflectHoldVfxRoot;
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private GameObject defaultLoopTrailRoot;
    [SerializeField] private GameObject shotRedirectTrailRoot;
    [SerializeField] private GameObject meleeReflectTrailRoot;
    [SerializeField] private GameObject orbitTrailRoot;
    [SerializeField] private GameObject failedTrailRoot;
    [SerializeField] private GameObject shotRedirectAuraRoot;

    private Color baseColor = Color.white;
    private Color returningColor = Color.white;
    private Color reflectableColor = Color.yellow;
    private Color orbitStartFlashColor = Color.cyan;

    private float reflectableFlashEndTime;
    private float orbitFlashEndTime;

    private float pulseStartTime;
    private float pulseEndTime;
    private float pulseScaleMultiplier = 1.35f;

    private BoomerangTrailVisualMode currentTrailMode = BoomerangTrailVisualMode.None;


    private bool shotRedirectAuraActive;
    private float shotRedirectAuraSpinSpeedDegPerSec;
    private float shotRedirectAuraAngle;


    private float shotRedirectAuraOrbitRadius;
    private float shotRedirectAuraSelfSpinSpeedDegPerSec;
    private float shotRedirectAuraRadiusPulseAmplitude;
    private float shotRedirectAuraRadiusPulseSpeed;

    private Color shotRedirectColor = new Color(0.35f, 0.85f, 1f, 1f);
    private Color meleeReflectColor = new Color(1f, 0.92f, 0.35f, 1f);
    private Color failedColor = new Color(1f, 0.35f, 0.35f, 1f);

    private Vector3 baseScale = Vector3.one;
    private BoomerangProjectileMotorState currentState = BoomerangProjectileMotorState.None;

    private void Awake()
    {
        if (spriteRenderer == null)
            spriteRenderer = GetComponentInChildren<SpriteRenderer>(true);



        if (spriteRenderer != null)
            baseColor = spriteRenderer.color;

        baseScale = transform.localScale;

        SetShotRedirectAuraActive(false, 0f);

        SetReturnWindowActive(false);
        SetReflectHoldActive(false);
        SetTrailMode(BoomerangTrailVisualMode.None);
    }

    private void Update()
    {
        UpdateColor();
        UpdatePulse();
        TickShotRedirectAuraSpin();
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

        SetShotRedirectAuraActive(false, 0f);

        SetTrailMode(BoomerangTrailVisualMode.None);
        currentTrailMode = BoomerangTrailVisualMode.None;
    }


    private void TickShotRedirectAuraSpin()
    {
        if (!shotRedirectAuraActive || shotRedirectAuraRoot == null)
            return;

        shotRedirectAuraAngle += shotRedirectAuraSpinSpeedDegPerSec * Time.deltaTime;

        float pulse =
            1f +
            Mathf.Sin(Time.time * shotRedirectAuraRadiusPulseSpeed) *
            shotRedirectAuraRadiusPulseAmplitude;

        float radius = shotRedirectAuraOrbitRadius * pulse;

        Vector3 localOffset = new Vector3(
            Mathf.Cos(shotRedirectAuraAngle * Mathf.Deg2Rad),
            Mathf.Sin(shotRedirectAuraAngle * Mathf.Deg2Rad),
            0f) * radius;

        shotRedirectAuraRoot.transform.localPosition = localOffset;
        shotRedirectAuraRoot.transform.localRotation =
            Quaternion.Euler(0f, 0f, shotRedirectAuraAngle * shotRedirectAuraSelfSpinSpeedDegPerSec);
    }

    public void SetMotorState(BoomerangProjectileMotorState state)
    {
        currentState = state;

        SetReflectHoldActive(state == BoomerangProjectileMotorState.ReflectHold);

        if (state == BoomerangProjectileMotorState.Orbiting)
        {
            SetTrailMode(BoomerangTrailVisualMode.OrbitReward);
            return;
        }

        if (state == BoomerangProjectileMotorState.DriftingLost)
        {
            SetTrailMode(BoomerangTrailVisualMode.Failed);
            return;
        }

        if (currentTrailMode == BoomerangTrailVisualMode.None ||
            currentTrailMode == BoomerangTrailVisualMode.OrbitReward ||
            currentTrailMode == BoomerangTrailVisualMode.Failed)
        {
            SetTrailMode(BoomerangTrailVisualMode.DefaultLoop);
        }
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

        if (currentTrailMode == BoomerangTrailVisualMode.ShotRedirect)
        {
            spriteRenderer.color = shotRedirectColor;
            return;
        }

        if (currentTrailMode == BoomerangTrailVisualMode.MeleeReflect)
        {
            spriteRenderer.color = meleeReflectColor;
            return;
        }

        if (currentTrailMode == BoomerangTrailVisualMode.Failed)
        {
            spriteRenderer.color = failedColor;
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

    private void SetTrailMode(BoomerangTrailVisualMode mode)
    {
        if (currentTrailMode == mode)
            return;

        currentTrailMode = mode;

        SetTrailRoot(defaultLoopTrailRoot, mode == BoomerangTrailVisualMode.DefaultLoop);
        SetTrailRoot(shotRedirectTrailRoot, mode == BoomerangTrailVisualMode.ShotRedirect);
        SetTrailRoot(meleeReflectTrailRoot, mode == BoomerangTrailVisualMode.MeleeReflect);
        SetTrailRoot(orbitTrailRoot, mode == BoomerangTrailVisualMode.OrbitReward);
        SetTrailRoot(failedTrailRoot, mode == BoomerangTrailVisualMode.Failed);
    }

    private void SetTrailRoot(GameObject root, bool active)
    {
        if (root == null)
            return;

        bool wasActive = root.activeSelf;

        if (active && !wasActive)
            ClearTrailRoot(root);

        if (wasActive != active)
            root.SetActive(active);
    }

    private void ClearTrailRoot(GameObject root)
    {
        if (root == null)
            return;

        TrailRenderer[] trails = root.GetComponentsInChildren<TrailRenderer>(true);
        for (int i = 0; i < trails.Length; i++)
            trails[i].Clear();
    }

   

    public void SetShotRedirectTrail()
    {
        SetTrailMode(BoomerangTrailVisualMode.ShotRedirect);
    }

    public void SetDefaultLoopTrail()
    {
        SetTrailMode(BoomerangTrailVisualMode.DefaultLoop);
        SetShotRedirectAuraActive(false, 0f, 0f);
    }

    public void SetMeleeReflectTrail()
    {
        SetTrailMode(BoomerangTrailVisualMode.MeleeReflect);
        SetShotRedirectAuraActive(false, 0f, 0f);
    }

    public void SetOrbitTrail()
    {
        SetTrailMode(BoomerangTrailVisualMode.OrbitReward);
        SetShotRedirectAuraActive(false, 0f, 0f);
    }

    public void SetFailedTrail()
    {
        SetTrailMode(BoomerangTrailVisualMode.Failed);
        SetShotRedirectAuraActive(false, 0f, 0f);
    }

    public void SetShotRedirectAuraActive(
    bool active,
    float orbitRadius = 0f,
    float orbitSpeedDegPerSec = 0f,
    float selfSpinMultiplier = 2f,
    float radiusPulseAmplitude = 0.08f,
    float radiusPulseSpeed = 18f)
    {
        shotRedirectAuraActive = active;
        shotRedirectAuraOrbitRadius = orbitRadius;
        shotRedirectAuraSpinSpeedDegPerSec = orbitSpeedDegPerSec;
        shotRedirectAuraSelfSpinSpeedDegPerSec = selfSpinMultiplier;
        shotRedirectAuraRadiusPulseAmplitude = radiusPulseAmplitude;
        shotRedirectAuraRadiusPulseSpeed = radiusPulseSpeed;

        if (!active)
        {
            shotRedirectAuraAngle = 0f;
        }

        if (shotRedirectAuraRoot != null)
        {
            shotRedirectAuraRoot.SetActive(active);

            if (!active)
            {
                shotRedirectAuraRoot.transform.localPosition = Vector3.zero;
                shotRedirectAuraRoot.transform.localRotation = Quaternion.identity;
            }
        }
    }
}