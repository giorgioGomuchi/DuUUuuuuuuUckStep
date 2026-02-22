public class DualWieldState : IWeaponState
{
    private WeaponSlotsController controller;

    public DualWieldState(WeaponSlotsController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        controller.ShowBoth();
    }

    public void Exit() { }

    public void FirePrimary()
    {
        controller.FireMain();
    }

    public void FireSecondary()
    {
        controller.FireSecondaryWeapon();
    }

    public void SwitchWeapon()
    {
        controller.SwapWeapons();
    }
}
