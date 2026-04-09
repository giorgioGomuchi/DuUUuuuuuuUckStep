using UnityEngine;

public interface IBoomerangSequenceBridge
{
    bool IsSequenceActive { get; }

    bool TryResolveMeleeReflect(BoomerangProjectile2D projectile, DeflectInfo info);

    void RegisterBoomerangDamage(BoomerangProjectile2D projectile, Collider2D other, BoomerangFlightState flightState);
}