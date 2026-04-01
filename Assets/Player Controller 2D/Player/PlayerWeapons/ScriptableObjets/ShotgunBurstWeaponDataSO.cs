using UnityEngine;

[CreateAssetMenu(menuName = "Game/Data/Weapons/Shotgun Burst")]
public class ShotgunBurstWeaponDataSO : ShotgunWeaponDataSO
{
    [Header("Burst Timing")]
    [Min(1)]
    public int burstShotCount = 3;

    [Min(0f)]
    public float shot1Delay = 0.2f;

    [Min(0f)]
    public float shot2Delay = 0.2f;

    [Min(0f)]
    public float postBurstRecoveryDelay = 0.4f;
}