using UnityEngine;

[CreateAssetMenu(fileName = "WeaponOverrideSequenceReward", menuName = "Game/Player/Timed Sequence Rewards/Weapon Override")]
public class WeaponOverrideSequenceRewardSO : SequenceRewardSO
{
    public enum OverrideRewardMode
    {
        Ammo = 0,
        Duration = 1
    }

    [Header("Override")]
    [SerializeField] private WeaponDataSO overrideWeaponData;
    [SerializeField] private WeaponSlotType targetSlot = WeaponSlotType.Main;
    [SerializeField] private OverrideRewardMode rewardMode = OverrideRewardMode.Duration;

    [Header("Ammo Mode")]
    [Min(1)]
    [SerializeField] private int baseAmmoCount = 3;

    [Header("Duration Mode")]
    [Min(0.05f)]
    [SerializeField] private float baseDurationSeconds = 3f;

    [Header("Performance Scaling")]
    [SerializeField] private bool scaleWithPerformance = true;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerPerfectShot = 0.15f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerHit = 0.08f;

    [Min(0f)]
    [SerializeField] private float bonusDurationPerUniqueTarget = 0.2f;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerPerfectShot = 0;

    [Min(0)]
    [SerializeField] private int bonusAmmoPerUniqueTarget = 0;

    [Header("Clamp")]
    [Min(0.1f)]
    [SerializeField] private float maxDurationSeconds = 6f;

    [Min(1)]
    [SerializeField] private int maxAmmoCount = 12;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public override void Apply(WeaponSequenceRewardContext context)
    {
        if (context == null || context.PlayerReferences == null)
        {
            Debug.LogError("[WeaponOverrideSequenceRewardSO] Invalid context.");
            return;
        }

        WeaponOverrideController overrideController = context.PlayerReferences.WeaponOverride;
        if (overrideController == null)
        {
            Debug.LogError("[WeaponOverrideSequenceRewardSO] WeaponOverrideController missing.");
            return;
        }

        if (overrideWeaponData == null)
        {
            Debug.LogError("[WeaponOverrideSequenceRewardSO] Override weapon data missing.");
            return;
        }

        WeaponSequencePerformance performance = context.Performance;

        if (rewardMode == OverrideRewardMode.Ammo)
        {
            int finalAmmo = ResolveAmmo(performance);

            overrideController.ApplyTemporaryWeaponOverride(targetSlot, overrideWeaponData, finalAmmo);

            if (debugLogs)
            {
                Debug.Log(
                    $"[WeaponOverrideSequenceRewardSO] Applied AMMO reward -> {overrideWeaponData.weaponName} | Ammo={finalAmmo}",
                    context.PlayerReferences);
            }
        }
        else
        {
            float finalDuration = ResolveDuration(performance);

            overrideController.ApplyTemporaryWeaponOverrideForDuration(targetSlot, overrideWeaponData, finalDuration);

            if (debugLogs)
            {
                Debug.Log(
                    $"[WeaponOverrideSequenceRewardSO] Applied DURATION reward -> {overrideWeaponData.weaponName} | Duration={finalDuration:F2}s",
                    context.PlayerReferences);
            }
        }
    }

    private int ResolveAmmo(WeaponSequencePerformance performance)
    {
        int ammo = baseAmmoCount;

        if (scaleWithPerformance && performance != null)
        {
            ammo += performance.PerfectShots * bonusAmmoPerPerfectShot;
            ammo += performance.UniqueTargetsHitCount * bonusAmmoPerUniqueTarget;
        }

        return Mathf.Clamp(ammo, 1, maxAmmoCount);
    }

    private float ResolveDuration(WeaponSequencePerformance performance)
    {
        float duration = baseDurationSeconds;

        if (scaleWithPerformance && performance != null)
        {
            duration += performance.PerfectShots * bonusDurationPerPerfectShot;
            duration += performance.TotalHits * bonusDurationPerHit;
            duration += performance.UniqueTargetsHitCount * bonusDurationPerUniqueTarget;
        }

        return Mathf.Clamp(duration, 0.05f, maxDurationSeconds);
    }
}