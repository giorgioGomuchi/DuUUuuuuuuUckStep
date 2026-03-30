using UnityEngine;

[CreateAssetMenu(
    fileName = "WeaponOverrideRewardApply",
    menuName = "Game/Player/Timed Sequence Rewards/Weapon Override Apply")]
public class WeaponOverrideRewardApplySO : SequenceRewardSOBase
{
    [Header("Override")]
    [SerializeField] private WeaponDataSO overrideWeaponData;
    [SerializeField] private WeaponSlotType targetSlot = WeaponSlotType.Main;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public override void Apply(
        SequenceRewardContextBase context,
        SequenceRewardResolution resolution,
        ISequenceActorAdapter actorAdapter)
    {
        if (!resolution.shouldApply)
            return;

        if (overrideWeaponData == null)
        {
            Debug.LogError("[WeaponOverrideRewardApplySO] Override weapon data missing.");
            return;
        }

        if (actorAdapter is not WeaponSequenceActorAdapter weaponAdapter)
        {
            Debug.LogError("[WeaponOverrideRewardApplySO] Invalid actor adapter.");
            return;
        }

        WeaponOverrideController overrideController = weaponAdapter.OverrideController;
        if (overrideController == null)
        {
            Debug.LogError("[WeaponOverrideRewardApplySO] WeaponOverrideController missing.");
            return;
        }

        if (resolution.ammo > 0)
        {
            overrideController.ApplyTemporaryWeaponOverride(targetSlot, overrideWeaponData, resolution.ammo);

            if (debugLogs)
            {
                Debug.Log(
                    $"[WeaponOverrideRewardApplySO] Applied AMMO reward -> {overrideWeaponData.weaponName} | Ammo={resolution.ammo}",
                    overrideController);
            }

            return;
        }

        if (resolution.duration > 0f)
        {
            overrideController.ApplyTemporaryWeaponOverrideForDuration(targetSlot, overrideWeaponData, resolution.duration);

            if (debugLogs)
            {
                Debug.Log(
                    $"[WeaponOverrideRewardApplySO] Applied DURATION reward -> {overrideWeaponData.weaponName} | Duration={resolution.duration:F2}s",
                    overrideController);
            }
        }
    }
}