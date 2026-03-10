using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Hitscan")]
public class HitscanWeaponDataSO : RangedWeaponDataSO
{
    [Header("Hitscan")]
    public float range = 18f;

    [Min(1)]
    public int maxTargets = 1;

    [Tooltip("If true, stops at the first collider even if it is not damageable.")]
    public bool stopAtFirstCollider = true;

    [Header("Visual")]
    public GameObject tracerPrefab;
    public float tracerDuration = 0.05f;
}