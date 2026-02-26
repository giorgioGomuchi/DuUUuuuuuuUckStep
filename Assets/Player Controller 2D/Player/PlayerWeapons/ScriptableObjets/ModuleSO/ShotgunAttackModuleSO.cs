using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Shotgun")]
public class ShotgunAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override void Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (data is not ShotgunWeaponDataSO shotgun)
        {
            Debug.LogError("[ShotgunAttackModuleSO] Wrong WeaponData type.", weapon);
            return;
        }

        if (shotgun.projectilePrefab == null)
        {
            Debug.LogError("[ShotgunAttackModuleSO] projectilePrefab is null.", weapon);
            return;
        }

        int baseDamage = weapon.ConsumeFinalDamage(shotgun.damage);
        int pellets = Mathf.Max(1, shotgun.pellets);

        for (int i = 0; i < pellets; i++)
        {
            Vector2 dir = GetPelletDirection(weapon.CurrentAim, shotgun, i, pellets);
            float speed = Random.Range(shotgun.minPelletSpeed, shotgun.maxPelletSpeed);

            SpawnPellet(weapon, shotgun, dir, speed, baseDamage);
        }

        if (debugLogs)
            Debug.Log($"[ShotgunAttackModuleSO] Fired pellets={pellets} dmg={baseDamage}", weapon);
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

    private static void SpawnPellet(WeaponBehaviour weapon, ShotgunWeaponDataSO data, Vector2 dir, float speed, int damage)
    {
        GameObject go = Object.Instantiate(data.projectilePrefab, weapon.FirePoint.position, Quaternion.identity);

        var proj = go.GetComponent<KinematicProjectile2D>();
        if (proj == null)
        {
            Debug.LogError("[ShotgunAttackModuleSO] Projectile missing KinematicProjectile2D.", weapon);
            return;
        }

        proj.Initialize(dir, speed, damage, data.targetLayer);

        // Rebote paredes (regla independiente al deflect)
        if (data.enableWallBounce)
        {
            var bounce = go.GetComponent<IBounceConfigurable>();
            if (bounce != null)
                bounce.ConfigureBounce(data.wallLayer, data.maxBounces, data.bounceSpeedMultiplier);
        }
    }
}