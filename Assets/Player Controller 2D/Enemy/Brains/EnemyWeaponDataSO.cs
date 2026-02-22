using System.Collections.Generic;
using UnityEngine;
using static UnityEngine.GraphicsBuffer;

[CreateAssetMenu(menuName = "Game/Enemy/Weapon Data", fileName = "EnemyWeaponData")]
public class EnemyWeaponDataSO : ScriptableObject
{
    [Header("Projectile Spawn")]
    [Tooltip("Prefab del proyectil. Puede tener EnemyProjectile o Projectile (tu clase base).")]
    public GameObject projectilePrefab;

    [Tooltip("Velocidad base del proyectil.")]
    public float projectileSpeed = 8f;

    [Tooltip("Daño base (si el prefab usa EnemyProjectile).")]
    public int damage = 1;

    [Tooltip("LayerMask de objetivos válidos (Player, etc.).")]
    public LayerMask targetLayer;

    [Header("Cooldown")]
    public float cooldown = 1.0f;


    [Header("Modifiers Pipeline")]
    [Tooltip("Módulos que alteran el disparo (spread, explosión, láser, etc.). Se ejecutan en orden.")]
    public WeaponModifierSO[] modifiers;


}
