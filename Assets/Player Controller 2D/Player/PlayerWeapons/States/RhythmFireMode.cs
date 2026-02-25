using UnityEngine;

public sealed class RhythmFireMode : IFireMode
{
    private readonly RhythmCombatController rhythm;

    public RhythmFireMode(RhythmCombatController rhythm)
    {
        this.rhythm = rhythm;
    }

    public void TryFire(WeaponBehaviour weapon, CombatAction action)
    {
        if (weapon == null) return;

        // Fallback: no rhythm system available
        if (rhythm == null || weapon.WeaponData == null || !weapon.WeaponData.useRhythmGate)
        {
            weapon.TryFire();
            return;
        }

        RhythmInputResult result = rhythm.RegisterAttack(action);

        // Per-weapon cancel rule
        if (weapon.WeaponData.cancelAttackOnFail && result.quality == RhythmHitQuality.Fail)
            return;

        // Per-weapon Perfect bonus
        if (result.quality == RhythmHitQuality.Perfect)
            weapon.SetNextAttackDamageMultiplier(weapon.WeaponData.perfectDamageMultiplier);

        weapon.TryFire();
    }
}