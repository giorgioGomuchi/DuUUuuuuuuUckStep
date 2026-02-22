using UnityEngine;

public class WeaponSlotsController : MonoBehaviour
{
    [Header("Weapons")]
    [SerializeField] private WeaponBehaviour mainWeapon;
    [SerializeField] private WeaponBehaviour secondaryWeapon;
    [SerializeField] private bool allowDualWield;


    private IWeaponState currentState;
    private SingleWieldState singleState;
    private DualWieldState dualState;

    private Vector2 currentAim;

    private void Awake()
    {
        singleState = new SingleWieldState(this);
        dualState = new DualWieldState(this);

        if (allowDualWield)
            currentState = dualState;
        else
            currentState = singleState;

        currentState.Enter();   //CRÍTICO
    }

    #region State Control

    public void SetSingleWield()
    {
        currentState?.Exit();
        currentState = singleState;
        currentState.Enter();
    }

    public void SetDualWield()
    {
        currentState?.Exit();
        currentState = dualState;
        currentState.Enter();
    }

    #endregion

    #region Public API (called from PlayerRoot)

    public void FirePrimary() => currentState.FirePrimary();
    public void FireSecondary() => currentState.FireSecondary();
    public void SwitchWeapon() => currentState.SwitchWeapon();

    public void SetAim(Vector2 direction)
    {
        currentAim = direction;

        mainWeapon?.SetAim(direction);
        secondaryWeapon?.SetAim(direction);
    }

    #endregion

    #region Internal helpers used by states

    public void FireMain() => mainWeapon?.TryFire();
    public void FireSecondaryWeapon() => secondaryWeapon?.TryFire();

    public void SwapWeapons()
    {
        var temp = mainWeapon;
        mainWeapon = secondaryWeapon;
        secondaryWeapon = temp;

        SetAim(currentAim);
    }

    public void ShowMainOnly()
    {
        if (mainWeapon) mainWeapon.gameObject.SetActive(true);
        if (secondaryWeapon) secondaryWeapon.gameObject.SetActive(false);
    }

    public void ShowBoth()
    {
        if (mainWeapon) mainWeapon.gameObject.SetActive(true);
        if (secondaryWeapon) secondaryWeapon.gameObject.SetActive(true);
    }

    #endregion
}
