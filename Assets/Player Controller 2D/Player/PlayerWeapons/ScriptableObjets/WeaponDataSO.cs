using UnityEngine;



public class WeaponDataSO : ScriptableObject
{
    [Header("Attack Strategy")]
    public AttackModuleSO attackModule;

    [Header("Rhythm")]
    [Tooltip("If true, this weapon uses RhythmCombatController timing. If false, it fires with normal cooldown only.")]
    public bool useRhythmGate = false;

    [Tooltip("If true, a Fail timing can cancel the shot (only when rhythm gate is enabled).")]
    public bool cancelAttackOnFail = false;

    [Tooltip("Damage multiplier applied on Perfect timing (only when rhythm gate is enabled).")]
    [Range(1f, 3f)]
    public float perfectDamageMultiplier = 1.75f;

    [Header("General")]
    public string weaponName;
    public Sprite weaponIcon;          // icono / sprite en mano
    public int damage = 1;
    public float cooldown = 0.5f;

    [Header("Targeting")]
    public LayerMask targetLayer;

    [Header("Camera Feedback")]
    public float cameraShakeDuration = 0.1f;
    public float cameraShakeStrength = 0.15f;

    [Header("knockback Force")]
    public float knockbackForce = 2.5f;
}
