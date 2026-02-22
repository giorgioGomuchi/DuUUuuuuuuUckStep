using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [SerializeField] private PlayerInputReader input;
    [SerializeField] private PlayerAim aim;
    [SerializeField] private PlayerVisualController visual;
    [SerializeField] private WeaponSlotsController weapons;

    private void Awake()
    {
        input.AimEvent += aim.SetAim;

        aim.OnAimChanged += visual.SetAim;
        aim.OnAimChanged += weapons.SetAim;

        input.FirePrimaryEvent += weapons.FirePrimary;
        input.FireSecondaryEvent += weapons.FireSecondary;
        input.SwitchWeaponEvent += weapons.SwitchWeapon;
    }
}
