using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [SerializeField] private WeaponSlotsController weapons;

    public bool CombatBlocked { get; private set; }

    private void Awake()
    {
        if (weapons == null) weapons = GetComponentInChildren<WeaponSlotsController>();
    }

    public void SetAim(Vector2 dir) => weapons?.SetAim(dir);

    public void SetCombatBlocked(bool blocked)
    {
        CombatBlocked = blocked;
        if (blocked)
        {
            CancelAllAttacks();
        }
    }

    public void TickCombat(PlayerInputReader input)
    {
        if (weapons == null) return;
        if (CombatBlocked) return;

        // Held autofire
        if (input.FirePrimaryHeld) weapons.FirePrimary();
        if (input.FireSecondaryHeld) weapons.FireSecondary();

        if (input.ConsumeSwitchWeaponPressed())
            weapons.SwitchWeapon();
    }

    public void CancelAllAttacks()
    {
        weapons?.CancelAllAttacks();
    }
}