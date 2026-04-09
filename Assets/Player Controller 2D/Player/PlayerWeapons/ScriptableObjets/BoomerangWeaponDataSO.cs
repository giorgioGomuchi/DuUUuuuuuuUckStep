using UnityEngine;

[CreateAssetMenu(fileName = "BoomerangWeaponData", menuName = "Game/Boomerang/Boomerang Weapon Data")]
public class BoomerangWeaponDataSO : RangedWeaponDataSO
{
    [Header("Sequence")]
    public BoomerangSequenceDefinitionSO sequenceDefinition;

    [Header("Boomerang")]
    [SerializeField] private BoomerangProjectileConfig projectileConfig = new();

    public BoomerangProjectileConfig BuildProjectileConfig()
    {
        return new BoomerangProjectileConfig
        {
            outboundDistance = projectileConfig.outboundDistance,
            outboundDistanceAfterDeflect = projectileConfig.outboundDistanceAfterDeflect,
            deflectOnlyWhileReturning = projectileConfig.deflectOnlyWhileReturning,
            holdReflectAtOwnerCenter = projectileConfig.holdReflectAtOwnerCenter,

            timedReturnArcStrength = projectileConfig.timedReturnArcStrength,
            timedReturnPresentationDistance = projectileConfig.timedReturnPresentationDistance,
            driftDeceleration = projectileConfig.driftDeceleration,

            dashReturnSpeedMultiplierBonus = projectileConfig.dashReturnSpeedMultiplierBonus,
            dashReturnSteeringBonus = projectileConfig.dashReturnSteeringBonus,
            dashReflectSpeedMultiplierBonus = projectileConfig.dashReflectSpeedMultiplierBonus,

            destroyEnemyProjectileMask = projectileConfig.destroyEnemyProjectileMask,

            orbitStartRadius = projectileConfig.orbitStartRadius,
            orbitRadiusGrowthPerSecond = projectileConfig.orbitRadiusGrowthPerSecond,
            orbitMaxRadius = projectileConfig.orbitMaxRadius,
            orbitAngularSpeedDegPerSec = projectileConfig.orbitAngularSpeedDegPerSec,
            orbitSpeedMultiplier = projectileConfig.orbitSpeedMultiplier,
            orbitClockwise = projectileConfig.orbitClockwise,
            orbitContactDamageInterval = projectileConfig.orbitContactDamageInterval,

            returningColor = projectileConfig.returningColor,
            reflectableColor = projectileConfig.reflectableColor,
            reflectableFlashDuration = projectileConfig.reflectableFlashDuration,
            orbitStartFlashColor = projectileConfig.orbitStartFlashColor,
            orbitStartFlashDuration = projectileConfig.orbitStartFlashDuration,
            orbitStartPulseScaleMultiplier = projectileConfig.orbitStartPulseScaleMultiplier,
            orbitStartPulseDuration = projectileConfig.orbitStartPulseDuration,

            enableSpin = projectileConfig.enableSpin,
            spinDegPerSec = projectileConfig.spinDegPerSec
        };
    }

    public float DashReturnSpeedMultiplierBonus => projectileConfig.dashReturnSpeedMultiplierBonus;
    public float DashReturnSteeringBonus => projectileConfig.dashReturnSteeringBonus;
    public float DashReflectSpeedMultiplierBonus => projectileConfig.dashReflectSpeedMultiplierBonus;
}