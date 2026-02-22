using UnityEngine;
using Unity.AI.Navigation;

public class NavMeshBaker2D : MonoBehaviour
{
    [SerializeField] private NavMeshSurface surface;

    public void Build()
    {
        if (surface == null)
            surface = GetComponent<NavMeshSurface>();

        if (surface == null)
        {
            Debug.LogWarning("Missing NavMeshSurface.");
            return;
        }

        surface.BuildNavMesh();
    }
}
