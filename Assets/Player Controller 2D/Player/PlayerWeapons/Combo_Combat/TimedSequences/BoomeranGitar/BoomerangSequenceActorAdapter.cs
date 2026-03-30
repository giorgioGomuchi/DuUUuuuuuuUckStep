using UnityEngine;

public class BoomerangSequenceActorAdapter : MonoBehaviour
{
    [SerializeField] private BoomerangProjectile2D projectile;

    public BoomerangProjectile2D Projectile => projectile;
    public bool IsValid => projectile != null;

    private void Awake()
    {
        if (projectile == null)
            projectile = GetComponent<BoomerangProjectile2D>();
    }

    public void SetProjectile(BoomerangProjectile2D projectile)
    {
        this.projectile = projectile;
    }

    public void BeginReturn(float duration, float reflectActivationNormalized)
    {
        if (projectile == null)
            return;

        projectile.StartCurvedReturn(duration, reflectActivationNormalized);
    }

    public void ResolveReflect(Vector2 direction)
    {
        if (projectile == null)
            return;

        projectile.ReflectFromMelee(direction);
    }

    public void BeginReward(float duration, int turns)
    {
        if (projectile == null)
            return;

        projectile.BeginOrbitReward(duration, turns);
    }

    public void FailAndCleanup(float destroyDelay)
    {
        if (projectile == null)
            return;

        projectile.EnterDriftLost();

        if (destroyDelay <= 0f)
            Destroy(projectile.gameObject);
        else
            Destroy(projectile.gameObject, destroyDelay);
    }

    public bool CanReceiveReflect()
    {
        return projectile != null && projectile.IsReflectable;
    }
}