using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI; // solo si usas NavMeshSurface (NavMeshPlus también)

public class LevelGenerationController : MonoBehaviour
{
    [Header("Generator")]
    [SerializeField] private ProceduralLevelGenerator generator;

    [Header("Builder")]
    [SerializeField] private LevelBuilder builder;

    [Header("Spawning")]
    [SerializeField] private SpawnSystem spawner;

    [Header("Level Flow")]
    [SerializeField] private EnemyRegistry registry;
    [SerializeField] private PortalSpawner portalSpawner;

    [Header("NavMesh 2D (NavMeshPlus)")]
    [SerializeField] private NavMeshBaker2D navMeshBaker;

    private void Start()
    {
        //GenerateNewLevel();
        GenerateLevel();

    }

    private void OnEnable()
    {
        registry.OnAllEnemiesDefeated += HandleLevelCleared;
    }

    private void HandleLevelCleared(Vector2 pos)
    {
        portalSpawner.SpawnPortal(pos);
    }

    public void GenerateNewLevel()
    {
        builder.ClearLevel();

        var grid = generator.Generate();
        var walkables = builder.Build(grid);

        spawner.SpawnAll(walkables);

        // Importante: NavMesh se reconstruye al final
        navMeshBaker.Build();
    }

    private void OnDisable()
    {
        if (registry != null)
            registry.OnAllEnemiesDefeated -= HandleLevelCleared;
    }

    public void GenerateLevel()
    {
        //builder.ClearLevel(); // si quieres reiniciar
        Debug.Log("Generating Level...");

        var grid = generator.Generate();

        var walkables = builder.Build(grid);

        spawner.SpawnAll(walkables);

        navMeshBaker.Build();
    }
}
