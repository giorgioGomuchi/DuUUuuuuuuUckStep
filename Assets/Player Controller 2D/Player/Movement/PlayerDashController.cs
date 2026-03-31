using UnityEngine;

public class PlayerDashController : MonoBehaviour
{
    [Header("Config")]
    [SerializeField] private PlayerConfigSO config;

    [Header("References")]
    [SerializeField] private PlayerMovement movement;
    [SerializeField] private PlayerHealth health;
    [SerializeField] private DashVfxController dashVfx;
    [SerializeField] private PlayerReferences playerReferences;

    private Vector2 dashDirection = Vector2.right;
    private float startTime;
    private float endTime;
    private bool isDashing;

    public bool IsDashing => isDashing;
    public float StartTime => startTime;
    public float EndTime => endTime;

    public float DashDistance => config != null ? config.dashDistance : 3.5f;
    public float DashDuration => config != null ? config.dashDuration : 0.12f;
    public float DashCooldown => config != null ? config.dashCooldown : 0.45f;

    private void Awake()
    {
        if (movement == null) movement = GetComponentInChildren<PlayerMovement>();
        if (health == null) health = GetComponentInParent<PlayerHealth>();
        if (dashVfx == null) dashVfx = GetComponentInChildren<DashVfxController>();
        if (playerReferences == null) playerReferences = GetComponentInParent<PlayerReferences>();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        if (movement == null) movement = GetComponentInChildren<PlayerMovement>();
        if (health == null) health = GetComponentInParent<PlayerHealth>();
        if (dashVfx == null) dashVfx = GetComponentInChildren<DashVfxController>();
        if (playerReferences == null) playerReferences = GetComponentInParent<PlayerReferences>();
    }
#endif

    public void SetConfig(PlayerConfigSO playerConfig)
    {
        config = playerConfig;
    }

    public void StartDash(Vector2 direction)
    {
        dashDirection = direction.sqrMagnitude > 0.0001f ? direction.normalized : Vector2.right;

        startTime = Time.time;
        endTime = startTime + Mathf.Max(0.01f, DashDuration);
        isDashing = true;

        dashVfx?.Play();

        if (health != null)
            health.SetInvulnerable(true);

        ApplyDashVelocity();

        // Notifica al sistema modular de secuencia SOLO cuando el dash ya ha arrancado.
        playerReferences?.WeaponSequenceControllerV2?.NotifySuccessfulDashDuringSequence();
    }

    public void Tick()
    {
        if (!isDashing) return;

        if (Time.time >= endTime)
            isDashing = false;
    }

    public void FixedTick()
    {
        if (!isDashing) return;
        ApplyDashVelocity();
    }

    public void StopDash()
    {
        movement?.ReleaseVelocityOverride();

        if (health != null)
            health.SetInvulnerable(false);

        dashVfx?.Stop();
        dashVfx?.ClearImmediately();

        isDashing = false;
    }

    public float GetNormalizedTime()
    {
        if (!isDashing) return 1f;
        return Mathf.InverseLerp(startTime, endTime, Time.time);
    }

    private void ApplyDashVelocity()
    {
        if (movement == null) return;

        float speed = DashDistance / Mathf.Max(0.01f, DashDuration);
        movement.ForceVelocity(dashDirection * speed);
    }
}