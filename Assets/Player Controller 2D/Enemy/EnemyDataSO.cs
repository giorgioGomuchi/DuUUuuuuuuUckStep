using UnityEngine;

public enum EnemyAttackType
{
    Melee,
    Ranged
}

[CreateAssetMenu(menuName = "Game/Data/Enemy Data", fileName = "EnemyData")]
public class EnemyDataSO : ScriptableObject
{
    // =========================
    // Identity
    // =========================

    [Header("Identity")]
    public string enemyName;
    public Sprite sprite;
    public EnemyAttackType attackType;

    // =========================
    // Core Stats
    // =========================

    [Header("Stats")]
    public float maxHealth = 10f;
    public float damage = 1f;

    // =========================
    // Vision & Detection
    // =========================

    [Header("Vision")]
    public float viewDistance = 6f;     // rango máximo para detectar jugador
    public float chaseRange = 5f;       // distancia óptima de combate

    // =========================
    // Movement
    // =========================

    [Header("Movement")]
    public float moveSpeed = 2f;        // velocidad normal
    public float chaseSpeed = 3f;       // opcional si luego quieres diferenciarlas
    public float stopDistance = 0.6f;   // distancia mínima antes de retroceder

    // =========================
    // Idle / Wander Behaviour
    // =========================

    [Header("Idle / Wander")]
    public float idleTime = 0.6f;
    public float wanderTime = 1.8f;
    [Range(0f, 1f)] public float wanderChance = 0.75f;
    public float wanderRadius = 3f;

    // =========================
    // Generic Attack
    // =========================

    [Header("Attack")]
    public float attackCooldown = 1f;
    public LayerMask targetLayerProyectile;

    // =========================
    // Melee Settings
    // =========================

    [Header("Melee Settings")]
    public float meleeRange = 0.5f;

    // =========================
    // Sniper Settings
    // =========================

    [Header("Sniper - Aim")]
    public float sniperAimDuration = 0.6f;      // tiempo apuntando antes de disparar
    public float sniperBlinkInterval = 0.08f;   // intervalo del parpadeo del láser

    [Header("Sniper - Burst")]
    public int sniperProjectileCount = 1;       // número de balas por ráfaga
    public float sniperSpreadAngle = 8f;        // ángulo total del abanico
    public float sniperTimeBetweenShots = 0.03f;

    [Header("Sniper - Projectile")]
    public GameObject sniperProjectilePrefab;
    public float sniperProjectileSpeed = 12f;
}
