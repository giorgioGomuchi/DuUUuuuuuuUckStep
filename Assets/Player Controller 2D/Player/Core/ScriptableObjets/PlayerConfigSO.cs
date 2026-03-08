using UnityEngine;

[CreateAssetMenu(fileName = "PlayerConfig", menuName = "Game/Player/Player Config")]
public class PlayerConfigSO : ScriptableObject
{
    [Header("Movement")]
    [Min(0f)] public float moveSpeed = 5f;

    [Header("Dash")]
    [Min(0f)] public float dashDistance = 3.5f;
    [Min(0.01f)] public float dashDuration = 0.12f;
    [Min(0f)] public float dashCooldown = 0.45f;

    [Header("Dash Cancel")]
    public bool allowCancelDashWithPrimary = false;
    public bool allowCancelDashWithSecondary = false;
    public bool allowCancelDashWithSwitchWeapon = false;

    [Range(0f, 1f)] public float dashCancelOpensAtNormalized = 0.35f;

    [Header("Health")]
    [Min(1)] public int maxHealth = 10;
}