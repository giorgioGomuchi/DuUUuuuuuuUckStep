using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Refs")]
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

    public void SetAim(Vector2 dir)
    {
        weapons?.SetAim(dir);
    }

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

        if (weapons == null || CombatBlocked)
            return;

        HandlePrimary(input);
        HandleSecondary(input);
        HandleSwitch(input);
    }

    private void HandlePrimary(PlayerInputReader input)
    {
        WeaponDataSO mainData = weapons.GetCurrentWeaponData(WeaponSlotType.Main);
        if (mainData == null)
            return;

        if (WeaponFireRequestUtility.ShouldFirePrimaryThisFrame(input, mainData))
            weapons.FirePrimary();
    }

    private void HandleSecondary(PlayerInputReader input)
    {
        WeaponDataSO secondaryData = weapons.GetCurrentWeaponData(WeaponSlotType.Secondary);
        if (secondaryData == null)
            return;

        if (WeaponFireRequestUtility.ShouldFireSecondaryThisFrame(input, secondaryData))
            weapons.FireSecondary();
    }

    private void HandleSwitch(PlayerInputReader input)
    {
        if (input.ConsumeSwitchWeaponPressed())
            weapons.SwitchWeapon();
    }

    public void CancelAllAttacks()
    {
        weapons?.CancelAllAttacks();
    }
}