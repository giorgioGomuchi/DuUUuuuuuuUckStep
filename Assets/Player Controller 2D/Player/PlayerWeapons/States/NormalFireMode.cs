public sealed class NormalFireMode : IFireMode
{
    public bool TryFire(WeaponBehaviour weapon, CombatAction action)
    {
        if (weapon == null)
            return false;

        return weapon.TryFire();
    }
}