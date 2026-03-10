using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [SerializeField] private WeaponSlotsController weapons;
    [SerializeField] private WeaponSequenceController weaponSequence;

    public bool CombatBlocked { get; private set; }

    private void Awake()
    {
        if (weapons == null)
            weapons = GetComponentInChildren<WeaponSlotsController>();

        if (weaponSequence == null)
            weaponSequence = GetComponentInChildren<WeaponSequenceController>();
    }

    public void SetAim(Vector2 dir) => weapons?.SetAim(dir);

    public void SetCombatBlocked(bool blocked)
    {
        CombatBlocked = blocked;
        if (blocked)
            CancelAllAttacks();
    }

    public void TickCombat(PlayerInputReader input)
    {
        if (input == null)
            return;

        if (weaponSequence != null && weaponSequence.IsSequenceActive)
        {
            weaponSequence.TickSequence(input);
            return;
        }

        if (weapons == null) return;
        if (CombatBlocked) return;

        if (input.FirePrimaryHeld)
            weapons.FirePrimary();

        if (input.FireSecondaryHeld)
            weapons.FireSecondary();

        if (input.ConsumeSwitchWeaponPressed())
            weapons.SwitchWeapon();
    }

    public void CancelAllAttacks()
    {
        weapons?.CancelAllAttacks();
    }
}