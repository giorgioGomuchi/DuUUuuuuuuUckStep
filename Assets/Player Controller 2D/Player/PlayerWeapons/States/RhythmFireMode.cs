using UnityEngine;

public sealed class RhythmFireMode : IFireMode
{
    private readonly RhythmCombatController rhythm;

    public RhythmFireMode(RhythmCombatController rhythm)
    {
        this.rhythm = rhythm;
    }

    public bool TryFire(WeaponBehaviour weapon, CombatAction action)
    {
        if (weapon == null)
            return false;

        // Fallback: no rhythm system available
        if (rhythm == null || weapon.WeaponData == null || !weapon.WeaponData.useRhythmGate)
        {
            return weapon.TryFire();
        }

        RhythmInputResult result = rhythm.RegisterAttack(action);

        // Si el arma cancela en fail y el resultado es fail, no se ejecuta el disparo
        if (weapon.WeaponData.cancelAttackOnFail && result.quality == RhythmHitQuality.Fail)
            return false;

        // Bonus de daño en perfect
        if (result.quality == RhythmHitQuality.Perfect)
            weapon.SetNextAttackDamageMultiplier(weapon.WeaponData.perfectDamageMultiplier);

        return weapon.TryFire();
    }
}