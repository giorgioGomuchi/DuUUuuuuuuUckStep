using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Ranged/Explosive")]
public class ExplosiveWeaponDataSO : RangedWeaponDataSO
{
    [Header("Explosion")]
    public float explosionRadius = 2.5f;
    public int explosionDamage = 3;
    public float explosionForce = 5f;
    public GameObject explosionPrefab;
}
