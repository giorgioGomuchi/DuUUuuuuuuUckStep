using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Shotgun")]
public class ShotgunAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (data is not ShotgunWeaponDataSO shotgun)
        {
            Debug.LogError("[ShotgunAttackModuleSO] Wrong WeaponData type.", weapon);
            return false;
        }

        if (shotgun.projectilePrefab == null)
        {
            Debug.LogError("[ShotgunAttackModuleSO] projectilePrefab is null.", weapon);
            return false;
        }

        int baseDamage = weapon.ConsumeFinalDamage(shotgun.damage);
        int pellets = Mathf.Max(1, shotgun.pellets);

        ShotgunSequenceController activeSequence = ResolveActiveShotgunSequenceController(weapon);

        for (int i = 0; i < pellets; i++)
        {
            Vector2 dir = GetPelletDirection(weapon.CurrentAim, shotgun, i, pellets);
            float speed = Random.Range(shotgun.minPelletSpeed, shotgun.maxPelletSpeed);

            SpawnPellet(weapon, shotgun, dir, speed, baseDamage, activeSequence);
        }

        if (debugLogs)
            Debug.Log($"[ShotgunAttackModuleSO] Fired pellets={pellets} dmg={baseDamage}", weapon);

        return true;
    }

    private static Vector2 GetPelletDirection(Vector2 aim, ShotgunWeaponDataSO data, int pelletIndex, int pellets)
    {
        float halfSpread = data.spreadAngleDegrees * 0.5f;

        float offset;
        if (data.randomSpread || pellets <= 1)
        {
            offset = Random.Range(-halfSpread, halfSpread);
        }
        else
        {
            float t = (pellets == 1) ? 0.5f : (float)pelletIndex / (pellets - 1);
            offset = Mathf.Lerp(-halfSpread, halfSpread, t);
        }

        float baseAngle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        float finalAngle = baseAngle + offset;

        return new Vector2(
            Mathf.Cos(finalAngle * Mathf.Deg2Rad),
            Mathf.Sin(finalAngle * Mathf.Deg2Rad)
        ).normalized;
    }

    private static void SpawnPellet(
        WeaponBehaviour weapon,
        ShotgunWeaponDataSO data,
        Vector2 dir,
        float speed,
        int damage,
        ShotgunSequenceController activeSequence)
    {
        GameObject go = Object.Instantiate(data.projectilePrefab, weapon.FirePoint.position, Quaternion.identity);

        KinematicProjectile2D proj = go.GetComponent<KinematicProjectile2D>();
        if (proj == null)
        {
            Debug.LogError("[ShotgunAttackModuleSO] Projectile missing KinematicProjectile2D.", weapon);
            return;
        }

        IgnoreOwnerCollisions(go, weapon);

        proj.Initialize(dir, speed, damage, data.targetLayer);

        if (proj is ShotgunSequencePelletProjectile shotgunPellet)
            shotgunPellet.ConfigurePellet(data, activeSequence);

        if (data.enableWallBounce)
        {
            IBounceConfigurable bounce = go.GetComponent<IBounceConfigurable>();
            if (bounce != null)
                bounce.ConfigureBounce(data.wallLayer, data.maxBounces, data.bounceSpeedMultiplier);
        }
    }

    private static void IgnoreOwnerCollisions(GameObject projectile, WeaponBehaviour weapon)
    {
        Collider2D projectileCol = projectile.GetComponent<Collider2D>();
        if (projectileCol == null || weapon == null)
            return;

        Collider2D[] ownerColliders = weapon.GetComponentsInParent<Collider2D>(true);
        for (int i = 0; i < ownerColliders.Length; i++)
        {
            if (ownerColliders[i] != null)
                Physics2D.IgnoreCollision(projectileCol, ownerColliders[i], true);
        }
    }

    private static ShotgunSequenceController ResolveActiveShotgunSequenceController(WeaponBehaviour weapon)
    {
        if (weapon == null)
            return null;

        PlayerReferences refs = weapon.GetComponentInParent<PlayerReferences>();
        if (refs == null || refs.Combat == null)
            return null;

        ShotgunSequenceController controller = refs.Combat.GetComponentInChildren<ShotgunSequenceController>(true);
        if (controller == null || !controller.IsSequenceActive)
            return null;

        return controller;
    }
}