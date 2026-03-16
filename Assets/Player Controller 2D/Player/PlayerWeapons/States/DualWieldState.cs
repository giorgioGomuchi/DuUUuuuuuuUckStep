public class DualWieldState : IWeaponState
{
    private readonly WeaponSlotsController controller;

    public DualWieldState(WeaponSlotsController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        controller.SetAllSlotVisualsVisible(true);
    }

    public void Exit()
    {
    }

    public void FirePrimary()
    {
        controller.TryFireSlot(WeaponSlotType.Main);
    }

    public void FireSecondary()
    {
        controller.TryFireSlot(WeaponSlotType.Secondary);
    }

    public void SwitchWeapon()
    {
        controller.PerformWeaponSwap();
    }
}