using UnityEngine;

[System.Serializable]
public class BoomerangProjectileConfig
{
    [Header("Core")]
    [Min(0.1f)] public float outboundDistance = 12f;
    [Min(0.1f)] public float outboundDistanceAfterDeflect = 4.5f;
    public bool deflectOnlyWhileReturning = true;
    public bool holdReflectAtOwnerCenter = true;

    [Header("Return Presentation")]
    [Min(0f)] public float timedReturnArcStrength = 0.08f;
    [Min(0f)] public float timedReturnPresentationDistance = 0.55f;
    [Min(0f)] public float driftDeceleration = 18f;

    [Header("Return Dash Bonuses")]
    [Min(0f)] public float dashReturnSpeedMultiplierBonus = 0.35f;
    [Min(0f)] public float dashReturnSteeringBonus = 5f;
    [Min(0.01f)] public float dashReflectSpeedMultiplierBonus = 1.35f;

    [Header("Projectile Interaction")]
    public LayerMask destroyEnemyProjectileMask;

    [Header("Orbit Reward")]
    [Min(0.05f)] public float orbitStartRadius = 0.8f;
    [Min(0f)] public float orbitRadiusGrowthPerSecond = 1f;
    [Min(0.05f)] public float orbitMaxRadius = 4f;
    [Min(1f)] public float orbitAngularSpeedDegPerSec = 360f;
    [Min(0.01f)] public float orbitSpeedMultiplier = 1.4f;
    public bool orbitClockwise = true;
    [Min(0.01f)] public float orbitContactDamageInterval = 0.2f;

    [Header("Visual Feedback")]
    public Color returningColor = Color.white;
    public Color reflectableColor = new(1f, 0.9f, 0.2f, 1f);
    [Min(0.01f)] public float reflectableFlashDuration = 0.12f;
    public Color orbitStartFlashColor = new(0.3f, 1f, 1f, 1f);
    [Min(0.01f)] public float orbitStartFlashDuration = 0.18f;
    [Min(1f)] public float orbitStartPulseScaleMultiplier = 1.35f;
    [Min(0.01f)] public float orbitStartPulseDuration = 0.2f;

    [Header("Spin")]
    public bool enableSpin = true;
    [Min(0f)] public float spinDegPerSec = 720f;
}