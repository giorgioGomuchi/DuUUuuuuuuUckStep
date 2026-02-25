using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Shotgun")]
public class ShotgunWeaponDataSO : RangedWeaponDataSO
{
    [Header("Shotgun")]
    public int pellets = 6;
    public float spreadAngleDegrees = 18f;  // total spread
    public bool randomSpread = true;

    [Header("Pellet Speed Variation")]
    public float minPelletSpeed = 10f;
    public float maxPelletSpeed = 15f;

    [Header("Wall Bounce")]
    public bool enableWallBounce = true;
    public LayerMask wallLayer;
    public int maxBounces = 1;
    [Range(0.1f, 1f)] public float bounceSpeedMultiplier = 0.85f;
}