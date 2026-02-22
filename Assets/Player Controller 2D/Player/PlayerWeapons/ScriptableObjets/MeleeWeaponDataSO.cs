using UnityEngine;

public abstract class MeleeWeaponDataSO : WeaponDataSO
{
    [Header("Melee")]
    public float swingAngle = 120f;
    public float swingDuration = 0.15f;
}
