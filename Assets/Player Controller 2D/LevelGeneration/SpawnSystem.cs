using System.Collections.Generic;
using UnityEngine;

public class SpawnSystem : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private List<GameObject> enemyPrefabs;

    [Header("Counts")]
    [SerializeField] private int enemyCount = 8;

    [Header("Rules")]
    [SerializeField] private float minDistancePlayerEnemy = 6f;

    [Header("Registry")]
    [SerializeField] private EnemyRegistry registry;

    private GameObject currentPlayer;

    public void SpawnAll(List<Vector2> walkables)
    {
        if (registry == null) registry = FindFirstObjectByType<EnemyRegistry>();

        Vector2 playerPos = PickRandom(walkables);
        SpawnPlayer(playerPos);

        for (int i = 0; i < enemyCount; i++)
        {
            Vector2 pos = PickFarFrom(walkables, playerPos, minDistancePlayerEnemy);
            SpawnEnemy(pos);
        }
    }

    private void SpawnPlayer(Vector2 pos)
    {
        if (playerPrefab == null) return;

        if (currentPlayer != null)
            Destroy(currentPlayer);

        currentPlayer = Instantiate(playerPrefab, pos, Quaternion.identity);

        // SET CAMERA TARGET
        var camFollow = FindFirstObjectByType<CameraFollow2D>();
        if (camFollow != null)
            camFollow.SetTarget(currentPlayer.transform);
    }

    private void SpawnEnemy(Vector2 pos)
    {
        if (enemyPrefabs == null || enemyPrefabs.Count == 0) return;

        var prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Count)];
        var go = Instantiate(prefab, pos, Quaternion.identity);

        var health = go.GetComponent<EnemyHealth>();
        if (health != null && registry != null)
            registry.Register(health);
    }

    private Vector2 PickRandom(List<Vector2> list)
    {
        return list[Random.Range(0, list.Count)];
    }

    private Vector2 PickFarFrom(List<Vector2> list, Vector2 origin, float minDist)
    {
        // intento simple (KISS)
        for (int i = 0; i < 50; i++)
        {
            var p = PickRandom(list);
            if (Vector2.Distance(p, origin) >= minDist)
                return p;
        }
        return PickRandom(list);
    }
}
