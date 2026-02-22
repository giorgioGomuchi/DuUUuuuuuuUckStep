using UnityEngine;



public class WeaponDataSO : ScriptableObject
{
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
