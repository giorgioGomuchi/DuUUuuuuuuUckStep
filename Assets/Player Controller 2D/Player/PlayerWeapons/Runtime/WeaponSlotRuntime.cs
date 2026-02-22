using UnityEngine;

[System.Serializable]
public class WeaponSlotRuntime
{
    public WeaponSlotSO config;
    public WeaponBehaviour weapon;
    public bool isActive = true;

    public void SetActive(bool value)
    {
        isActive = value;
    }

    public void TryFire()
    {
        if (!isActive || weapon == null)
            return;

        weapon.TryFire();
    }

    public void SetAim(Vector2 direction)
    {
        if (weapon == null)
            return;

        weapon.SetAim(direction);
    }
}
