using System;
using System.Collections.Generic;
using UnityEngine;

public class EnemyRegistry : MonoBehaviour
{
    public event Action<Vector2> OnAllEnemiesDefeated;

    private readonly HashSet<EnemyHealth> alive = new();
    private Vector2 lastDeathPos;

    public void Register(EnemyHealth enemy)
    {
        if (enemy == null) return;

        alive.Add(enemy);
        enemy.OnEnemyDied += HandleEnemyDied;
    }

    private void HandleEnemyDied(EnemyHealth enemy)
    {
        if (!alive.Contains(enemy)) return;

        lastDeathPos = enemy.transform.position;

        enemy.OnEnemyDied -= HandleEnemyDied;
        alive.Remove(enemy);

        if (alive.Count == 0)
        {
            OnAllEnemiesDefeated?.Invoke(lastDeathPos);
        }
    }



}
