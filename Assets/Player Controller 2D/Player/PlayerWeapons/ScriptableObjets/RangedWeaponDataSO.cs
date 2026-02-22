using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Ranged")]
public class RangedWeaponDataSO : WeaponDataSO
{
    [Header("Ranged")]
    public GameObject projectilePrefab;
    public float projectileSpeed = 12f;
}
