using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Melee Animated")]
public class MeleeAnimatedWeaponDataSO : MeleeWeaponDataSO
{
    [Header("Hit")]
    [Tooltip("Prefab that represents the melee hit (collider + visual)")]
    public GameObject hitPrefab;

    [Tooltip("Distance from player to spawn the hit")]
    public float range = 1.2f;

    [Tooltip("Lifetime of the hit object")]
    public float hitLifetime = 0.2f;

    [Header("Visual")]
    [Tooltip("Optional animation played on the weapon itself")]
    public AnimationClip attackAnimation;

    [Header("Attack Feedback")]
    [Tooltip("Local rotation applied to the weapon when attacking (degrees)")]
    public float feedbackRotationAngle = -75f;

    [Tooltip("Time to rotate towards and back from the attack (seconds)")]
    public float feedbackRotationDuration = 0.08f;

    [Header("Visual")]
    public float spriteAngleOffset = 0f;
}
