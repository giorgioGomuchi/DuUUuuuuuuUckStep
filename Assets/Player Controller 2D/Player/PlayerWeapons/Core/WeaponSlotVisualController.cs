using UnityEngine;

/// Owns weapon slot visual visibility only.
/// Does not fire, swap, or know anything about combos / overrides.
public class WeaponSlotVisualController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private WeaponBehaviour mainWeapon;
    [SerializeField] private WeaponBehaviour secondaryWeapon;

    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    public WeaponBehaviour MainWeapon => mainWeapon;
    public WeaponBehaviour SecondaryWeapon => secondaryWeapon;

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
        if (mainWeapon == null || secondaryWeapon == null)
        {
            WeaponBehaviour[] weapons = GetComponentsInChildren<WeaponBehaviour>(true);

            foreach (WeaponBehaviour weapon in weapons)
            {
                if (weapon == null)
                    continue;

                if (mainWeapon == null && weapon.name.ToLower().Contains("main"))
                {
                    mainWeapon = weapon;
                    continue;
                }

                if (secondaryWeapon == null && weapon.name.ToLower().Contains("secondary"))
                {
                    secondaryWeapon = weapon;
                }
            }
        }
    }

    public void SetSlotVisible(WeaponSlotType slot, bool visible)
    {
        WeaponBehaviour weapon = GetWeapon(slot);
        if (weapon == null)
            return;

        weapon.SetVisualVisible(visible);

        if (debugLogs)
            Debug.Log($"[WeaponSlotVisualController] SetSlotVisible -> {slot} = {visible}", this);
    }

    public void SetAllVisible(bool visible)
    {
        if (mainWeapon != null)
            mainWeapon.SetVisualVisible(visible);

        if (secondaryWeapon != null)
            secondaryWeapon.SetVisualVisible(visible);

        if (debugLogs)
            Debug.Log($"[WeaponSlotVisualController] SetAllVisible = {visible}", this);
    }

    public void ShowMainOnly()
    {
        SetSlotVisible(WeaponSlotType.Main, true);
        SetSlotVisible(WeaponSlotType.Secondary, false);
    }

    public void ShowBoth()
    {
        SetAllVisible(true);
    }

    private WeaponBehaviour GetWeapon(WeaponSlotType slot)
    {
        return slot == WeaponSlotType.Main ? mainWeapon : secondaryWeapon;
    }
}