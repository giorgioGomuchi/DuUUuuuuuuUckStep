public class SingleWieldState : IWeaponState
{
    private WeaponSlotsController controller;

    public SingleWieldState(WeaponSlotsController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        controller.ShowMainOnly();
    }

    public void Exit() { }

    public void FirePrimary()
    {
        controller.FireMain();
    }

    public void FireSecondary()
    {
        // No hace nada en single
    }

    public void SwitchWeapon()
    {
        controller.SwapWeapons();
        controller.ShowMainOnly();
    }
}
