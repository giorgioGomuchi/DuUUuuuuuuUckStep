using UnityEngine;

public class PortalSpawner : MonoBehaviour
{
    [SerializeField] private EnemyRegistry registry;
    [SerializeField] private GameObject portalPrefab;

    private void Awake()
    {
        if (registry == null) registry = FindFirstObjectByType<EnemyRegistry>();
        if (registry != null) registry.OnAllEnemiesDefeated += SpawnPortal;
    }

    private void OnDestroy()
    {
        if (registry != null) registry.OnAllEnemiesDefeated -= SpawnPortal;
    }

    public void SpawnPortal(Vector2 pos)
    {
        if (portalPrefab == null) return;

        var go = Instantiate(portalPrefab, pos, Quaternion.identity);
        var p = go.transform.position; p.z = 0f; go.transform.position = p;
    }
}
