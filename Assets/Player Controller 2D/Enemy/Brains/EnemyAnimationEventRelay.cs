using UnityEngine;

/// <summary>
/// Va en el GameObject que tiene el Animator (tu hijo "Visual").
/// Recibe Animation Events y los reenvía al Brain del padre.
/// </summary>
public class EnemyAnimationEventRelay : MonoBehaviour
{
    private EnemyFlyingBazookaBrain flyingBazookaBrain;

    private void Awake()
    {
        flyingBazookaBrain = GetComponentInParent<EnemyFlyingBazookaBrain>();
        if (flyingBazookaBrain == null)
            Debug.LogWarning("[EnemyAnimationEventRelay] No EnemyFlyingBazookaBrain found in parents.", this);
    }

    // Animation Event en eugenia_lift
    public void OnLiftFinished()
    {
        flyingBazookaBrain?.OnLiftFinished();
    }

    // Animation Event en eugenia_land
    public void OnLandFinished()
    {
        flyingBazookaBrain?.OnLandFinished();
    }
}
