using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Ranged/Boomerang")]
public class BoomerangWeaponDataSO : RangedWeaponDataSO
{
    [Header("Boomerang Sequence")]
    public BoomerangSequenceDefinitionSO sequenceDefinition;

    [Header("Boomerang Flight")]
    public float outboundDistance = 6f;
    public float returnSpeedMultiplier = 1.15f;
    public float returnSteering = 8f;
    public float reflectableDistance = 1.15f;
    public float catchDistance = 0.25f;
    public float driftDeceleration = 18f;

    [Header("Timed Return Tuning")]
    public float timedReturnArcStrength = 0.08f;
    public float timedReturnCatchBias = 0.92f;

    [Header("Deflect Rules")]
    public bool deflectOnlyWhileReturning = true;
    public float outboundDistanceAfterDeflect = 4.5f;

    [Header("Spin")]
    public float spinDegPerSec = 720f;

    [Header("Reflectable Feedback")]
    public Color reflectableColor = new Color(1f, 0.9f, 0.2f, 1f);
    public float reflectableFlashDuration = 0.12f;

    [Header("Dash Bonuses")]
    public float dashReturnSpeedMultiplierBonus = 0.35f;
    public float dashReturnSteeringBonus = 5f;
    public float dashReflectSpeedMultiplierBonus = 1.35f;
}