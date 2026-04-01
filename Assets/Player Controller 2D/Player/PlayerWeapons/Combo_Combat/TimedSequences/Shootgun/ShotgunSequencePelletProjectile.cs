using UnityEngine;

public class ShotgunSequencePelletProjectile : PlayerProjectile
{
    [Header("Distance Falloff")]
    [SerializeField] private float fullDamageRange = 3f;
    [SerializeField] private float maxEffectiveRange = 10f;
    [SerializeField] private float minDamageMultiplierAtMaxRange = 0.2f;
    [SerializeField] private bool destroyPastMaxRange = false;

    private Vector2 spawnPosition;
    private ShotgunSequenceController shotgunSequenceController;

    protected override void Awake()
    {
        base.Awake();
        spawnPosition = transform.position;
    }

    private void OnEnable()
    {
        spawnPosition = transform.position;
    }

    public void ConfigurePellet(ShotgunWeaponDataSO shotgunData, ShotgunSequenceController controller = null)
    {
        shotgunSequenceController = controller;
        spawnPosition = transform.position;

        if (shotgunData != null)
        {
            fullDamageRange = shotgunData.fullDamageRange;
            maxEffectiveRange = shotgunData.maxEffectiveRange;
            minDamageMultiplierAtMaxRange = shotgunData.minDamageMultiplierAtMaxRange;
            destroyPastMaxRange = shotgunData.destroyPastMaxRange;
        }
    }

    public void ConfigureSequencePellet(
        ShotgunSequenceController controller,
        ShotgunWeaponDataSO shotgunData)
    {
        ConfigurePellet(shotgunData, controller);
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();

        if (!destroyPastMaxRange)
            return;

        float distanceTravelled = Vector2.Distance(spawnPosition, transform.position);
        if (distanceTravelled > maxEffectiveRange)
            Kill();
    }

    protected override void OnHit(Collider2D other)
    {
        if (!IsInTargetMask(other))
            return;

        int finalDamage = ComputeDamageWithDistanceFalloff();

        var damageable = other.GetComponentInParent<IDamageable>();
        if (damageable != null)
            damageable.TakeDamage(finalDamage);

        shotgunSequenceController?.RegisterPelletHit(other, finalDamage);
        Kill();
    }

    private int ComputeDamageWithDistanceFalloff()
    {
        float distanceTravelled = Vector2.Distance(spawnPosition, transform.position);

        if (distanceTravelled <= fullDamageRange)
            return damage;

        if (distanceTravelled >= maxEffectiveRange)
            return Mathf.Max(1, Mathf.RoundToInt(damage * minDamageMultiplierAtMaxRange));

        float t = Mathf.InverseLerp(fullDamageRange, maxEffectiveRange, distanceTravelled);
        float multiplier = Mathf.Lerp(1f, minDamageMultiplierAtMaxRange, t);

        return Mathf.Max(1, Mathf.RoundToInt(damage * multiplier));
    }
}