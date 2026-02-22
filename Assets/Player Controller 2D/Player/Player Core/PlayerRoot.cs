using UnityEngine;

public class PlayerRoot : MonoBehaviour
{
    [SerializeField] private PlayerAim aim;
    [SerializeField] private PlayerVisualController visual;
    [SerializeField] private WeaponSlotsController weapons;

    private void Awake()
    {
        PlayerInputReader.AimEvent += aim.SetAim;

        aim.OnAimChanged += visual.SetAim;
        aim.OnAimChanged += weapons.SetAim;

        PlayerInputReader.FirePrimaryEvent += weapons.FirePrimary;
        PlayerInputReader.FireSecondaryEvent += weapons.FireSecondary;
        PlayerInputReader.SwitchWeaponEvent += weapons.SwitchWeapon;
    }
}
