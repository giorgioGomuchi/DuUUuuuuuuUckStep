public class SingleWieldState : IWeaponState
{
    private readonly WeaponSlotsController controller;

    public SingleWieldState(WeaponSlotsController controller)
    {
        this.controller = controller;
    }

    public void Enter()
    {
        controller.SetSlotVisualVisible(WeaponSlotType.Main, true);
        controller.SetSlotVisualVisible(WeaponSlotType.Secondary, false);
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
        // Intentionally disabled in single wield mode.
    }

    public void SwitchWeapon()
    {
        controller.PerformWeaponSwap();
        controller.SetSlotVisualVisible(WeaponSlotType.Main, true);
        controller.SetSlotVisualVisible(WeaponSlotType.Secondary, false);
    }
}