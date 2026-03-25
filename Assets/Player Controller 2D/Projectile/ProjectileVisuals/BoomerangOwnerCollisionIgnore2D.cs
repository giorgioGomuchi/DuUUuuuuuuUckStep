using System.Collections.Generic;
using UnityEngine;

public class BoomerangOwnerCollisionIgnore2D : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] private bool debugLogs = false;

    private Collider2D[] projectileColliders;
    private readonly List<Collider2D> ignoredOwnerColliders = new();

    private void Awake()
    {
        projectileColliders = GetComponentsInChildren<Collider2D>(true);
    }

    public void Apply(Transform ownerRoot)
    {
        Restore();
        ignoredOwnerColliders.Clear();

        if (ownerRoot == null || projectileColliders == null || projectileColliders.Length == 0)
            return;

        int playerMeleeLayer = LayerMask.NameToLayer("PlayerMelee");
        Collider2D[] ownerColliders = ownerRoot.GetComponentsInChildren<Collider2D>(true);

        foreach (Collider2D ownerCol in ownerColliders)
        {
            if (ownerCol == null)
                continue;

            if (playerMeleeLayer >= 0 && ownerCol.gameObject.layer == playerMeleeLayer)
                continue;

            foreach (Collider2D projCol in projectileColliders)
            {
                if (projCol == null)
                    continue;

                Physics2D.IgnoreCollision(projCol, ownerCol, true);
            }

            ignoredOwnerColliders.Add(ownerCol);

            if (debugLogs)
                Debug.Log($"[BoomerangOwnerCollisionIgnore2D] Ignoring owner body collider: {ownerCol.name}", this);
        }
    }

    public void Restore()
    {
        if (projectileColliders == null || ignoredOwnerColliders.Count == 0)
            return;

        foreach (Collider2D ownerCol in ignoredOwnerColliders)
        {
            if (ownerCol == null)
                continue;

            foreach (Collider2D projCol in projectileColliders)
            {
                if (projCol == null)
                    continue;

                Physics2D.IgnoreCollision(projCol, ownerCol, false);
            }
        }

        ignoredOwnerColliders.Clear();
    }
}