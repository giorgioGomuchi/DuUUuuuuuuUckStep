using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Ranged/Boomerang")]
public class BoomerangWeaponDataSO : RangedWeaponDataSO
{
    [Header("Boomerang")]
    public float outboundDistance = 6f;
    public float returnSpeedMultiplier = 1.15f;

    [Header("Deflect Rules")]
    public bool deflectOnlyWhileReturning = true;
    public float outboundDistanceAfterDeflect = 4.5f; // “mini-salida” tras rebotarlo
}