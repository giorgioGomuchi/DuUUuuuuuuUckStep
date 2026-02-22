using UnityEngine;

/// <summary>
/// Módulo enchufable que puede:
/// - modificar el contexto antes de disparar (ej: generar múltiples direcciones para spread)
/// - configurar el proyectil tras instanciarlo (ej: explosión)
/// - añadir VFX/telemetría
/// 
/// Diseñado para ser "composición": añades comportamiento creando nuevos assets.
/// </summary>
public abstract class WeaponModifierSO : ScriptableObject
{
    /// <summary>
    /// Se llama al inicio de Fire(). Ideal para:
    /// - cambiar direcciones (spread, burst)
    /// - bloquear disparo si no cumple condiciones
    /// - setear parámetros temporales
    /// </summary>
    public virtual void BeforeFire(ref WeaponFireContext ctx) { }

    /// <summary>
    /// Se llama después de instanciar CADA proyectil. Ideal para:
    /// - configurar explosión en ExplosiveProjectile
    /// - añadir componentes
    /// - setear propiedades extra
    /// </summary>
    public virtual void AfterSpawnedProjectile(ref WeaponFireContext ctx, GameObject projectileInstance) { }

    /// <summary>
    /// Se llama al final de Fire(), después de spawnear todos los proyectiles.
    /// Ideal para:
    /// - VFX globales, sonidos, eventos
    /// </summary>
    public virtual void AfterFire(ref WeaponFireContext ctx) { }
}
