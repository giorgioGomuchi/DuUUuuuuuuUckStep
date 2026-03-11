public interface IFireMode
{
    bool TryFire(WeaponBehaviour weapon, CombatAction action);
}