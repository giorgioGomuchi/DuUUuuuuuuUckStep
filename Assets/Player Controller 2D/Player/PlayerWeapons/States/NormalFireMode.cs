public sealed class NormalFireMode : IFireMode
{
    public void TryFire(WeaponBehaviour weapon, CombatAction action)
    {
        if (weapon == null) return;
        weapon.TryFire();
    }
}