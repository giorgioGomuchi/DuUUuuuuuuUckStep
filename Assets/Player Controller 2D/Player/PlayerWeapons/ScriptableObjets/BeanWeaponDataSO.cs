using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Beam")]
public class BeamWeaponDataSO : WeaponDataSO
{
    [Header("Beam Shape")]
    [Min(1f)]
    public float maxRange = 60f;

    [Tooltip("Layers that stop the beam visually and physically.")]
    public LayerMask blockingMask;

    [Header("Beam Damage")]
    [Min(0.01f)]
    public float damageTickInterval = 0.12f;

    [Min(1)]
    public int damagePerTick = 1;

    [Min(1)]
    public int maxTargetsPerTick = 16;

    [Header("Beam Follow")]
    [Tooltip("Lower = more loose, higher = more responsive.")]
    [Min(0.1f)]
    public float aimSmoothSpeed = 12f;

    [Header("Visual")]
    public BeamView beamViewPrefab;
    [Min(0.01f)] public float beamWidth = 0.18f;
    [Min(0.01f)] public float beamEndWidth = 0.12f;
}