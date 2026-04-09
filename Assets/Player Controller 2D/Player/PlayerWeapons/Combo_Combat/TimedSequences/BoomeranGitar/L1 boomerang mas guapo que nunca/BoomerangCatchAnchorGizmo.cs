using UnityEngine;

public class BoomerangCatchAnchorGizmo : MonoBehaviour
{
    [Header("Catch Debug")]
    [SerializeField] private float catchRadius = 0.35f;
    [SerializeField] private Color gizmoColor = new Color(0.2f, 1f, 1f, 0.85f);
    [SerializeField] private bool drawWhenSelectedOnly = false;

    public float CatchRadius => catchRadius;

    private void OnDrawGizmos()
    {
        if (drawWhenSelectedOnly)
            return;

        DrawGizmo();
    }

    private void OnDrawGizmosSelected()
    {
        DrawGizmo();
    }

    private void DrawGizmo()
    {
        Gizmos.color = gizmoColor;
        Gizmos.DrawWireSphere(transform.position, catchRadius);

        Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 0.25f);
        Gizmos.DrawSphere(transform.position, catchRadius * 0.12f);
    }
}