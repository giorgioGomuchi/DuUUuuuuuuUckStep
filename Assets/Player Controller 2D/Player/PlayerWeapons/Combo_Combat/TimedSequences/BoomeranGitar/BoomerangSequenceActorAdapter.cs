using UnityEngine;

public class BoomerangSequenceActorAdapter : SequenceActorAdapter
{
    [SerializeField] private BoomerangProjectile2D projectile;

    public BoomerangProjectile2D Projectile => projectile;

    public override bool IsValid => projectile != null;

    private void Awake()
    {
        if (projectile == null)
            projectile = GetComponent<BoomerangProjectile2D>();
    }

    public void SetProjectile(BoomerangProjectile2D projectile)
    {
        this.projectile = projectile;
    }

    public override void BeginReturn(float duration, float reflectActivationNormalized)
    {
        if (projectile == null)
            return;

        projectile.StartCurvedReturn(duration, reflectActivationNormalized);
    }

    public override void ResolveReflect(Vector2 direction)
    {
        if (projectile == null)
            return;

        projectile.ReflectFromMelee(direction);
    }

    public override void BeginReward(float duration, int turns)
    {
        if (projectile == null)
            return;

        projectile.BeginOrbitReward(duration, turns);
    }

    public override void FailAndCleanup(float destroyDelay)
    {
        if (projectile == null)
            return;

        projectile.EnterDriftLost();

        if (destroyDelay <= 0f)
            Destroy(projectile.gameObject);
        else
            Destroy(projectile.gameObject, destroyDelay);
    }

    public override bool CanReceiveReflect()
    {
        return projectile != null && projectile.IsReflectable;
    }
}