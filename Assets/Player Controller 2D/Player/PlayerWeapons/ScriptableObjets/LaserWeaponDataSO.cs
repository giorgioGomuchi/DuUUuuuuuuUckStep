using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Laser")]
public class LaserWeaponDataSO : HitscanWeaponDataSO
{
    [Header("Laser")]
    [Tooltip("Extra width multiplier for the tracer only.")]
    public float tracerWidthMultiplier = 1.5f;
}