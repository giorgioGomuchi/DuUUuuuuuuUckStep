using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Shotgun")]
public class ShotgunWeaponDataSO : RangedWeaponDataSO
{
    [Header("Shotgun")]
    public int pellets = 6;
    public float spreadAngleDegrees = 18f;  // total spread
    public bool randomSpread = true;
}