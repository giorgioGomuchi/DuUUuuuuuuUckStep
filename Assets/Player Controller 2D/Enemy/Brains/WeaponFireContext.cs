using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Contexto mutable que viaja por el pipeline de WeaponModifiers.
/// Los modifiers pueden:
/// - modificar la lista de direcciones
/// - leer/escribir metadatos del disparo (shotIndex/shotCount)
/// - configurar proyectiles tras instanciar
/// </summary>
public struct WeaponFireContext
{
    // Input base (antes de modifiers)
    public Vector2 baseDirection;

    // Lista final de direcciones a disparar (los modifiers pueden reemplazarla)
    public List<Vector2> directions;

    // Metadata opcional (útil para spread tipo sniper)
    public int shotIndex;
    public int shotCount;

    // Datos de spawn
    public GameObject projectilePrefab;
    public Transform firePoint;

    // Parámetros de movimiento/target
    public float speed;
    public LayerMask targetLayer;

    // Back-reference al arma (por si un modifier necesita refs extra)
    public EnemyWeapon weapon;

    public void EnsureDirectionsList()
    {
        if (directions == null)
            directions = new List<Vector2>(4);
    }

    public void ResetDirectionsToBase()
    {
        EnsureDirectionsList();
        directions.Clear();
        directions.Add(baseDirection.normalized);
    }
}
