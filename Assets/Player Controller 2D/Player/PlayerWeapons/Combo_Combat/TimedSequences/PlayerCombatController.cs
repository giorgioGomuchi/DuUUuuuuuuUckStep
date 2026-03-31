using UnityEngine;

public class PlayerCombatController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponSlotsController weapons;
    [SerializeField] private BoomerangSequenceController boomerangSequence;
    [SerializeField] private WeaponSequenceControllerV2 weaponSequenceController;

    public bool CombatBlocked { get; private set; }
    public BoomerangSequenceController BoomerangSequence => boomerangSequence;

    private void Awake()
    {
        ResolveReferences();
    }

#if UNITY_EDITOR
    private void OnValidate()
    {
        ResolveReferences();
    }
#endif

    private void ResolveReferences()
    {
        if (weapons == null)
            weapons = GetComponentInChildren<WeaponSlotsController>(true);

        if (boomerangSequence == null)
            boomerangSequence = GetComponentInChildren<BoomerangSequenceController>(true);

        if (weaponSequenceController == null)
            weaponSequenceController = GetComponentInChildren<WeaponSequenceControllerV2>(true);
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

        if (weapons == null || CombatBlocked)
            return;

        if (weaponSequenceController != null && weaponSequenceController.IsSequenceActive)
            return;

        if (boomerangSequence != null && boomerangSequence.IsSequenceActive)
        {
            boomerangSequence.TickSequence(input);

            if (boomerangSequence.IsInOrbitReward)
            {
                HandlePrimary(input);
                HandleSecondary(input);
                HandleSwitch(input);
                return;
            }

            HandleSecondary(input);
            return;
        }

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