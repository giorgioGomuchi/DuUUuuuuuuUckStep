using UnityEngine;

[CreateAssetMenu(menuName = "Game/AttackModules/Shotgun Burst")]
public class ShotgunBurstAttackModuleSO : AttackModuleSO
{
    [SerializeField] private bool debugLogs = false;

    public override bool Execute(WeaponBehaviour weapon, WeaponDataSO data)
    {
        if (weapon == null || data == null)
            return false;

        if (data is not ShotgunBurstWeaponDataSO shotgun)
        {
            Debug.LogError("[ShotgunBurstAttackModuleSO] Wrong WeaponData type. Expected ShotgunBurstWeaponDataSO.", weapon);
            return false;
        }

        if (shotgun.projectilePrefab == null)
        {
            Debug.LogError("[ShotgunBurstAttackModuleSO] projectilePrefab is null.", weapon);
            return false;
        }

        if (!weapon.TryLockAttack())
            return false;

        BurstRunner runner = weapon.GetComponent<BurstRunner>();
        if (runner == null)
            runner = weapon.gameObject.AddComponent<BurstRunner>();

        runner.Begin(this, weapon, shotgun, debugLogs);
        return true;
    }

    private void FireSingleShot(WeaponBehaviour weapon, ShotgunBurstWeaponDataSO shotgun)
    {
        int baseDamage = weapon.ConsumeFinalDamage(shotgun.damage);
        int pellets = Mathf.Max(1, shotgun.pellets);

        for (int i = 0; i < pellets; i++)
        {
            Vector2 dir = GetPelletDirection(weapon.CurrentAim, shotgun, i, pellets);
            float speed = Random.Range(shotgun.minPelletSpeed, shotgun.maxPelletSpeed);
            SpawnPellet(weapon, shotgun, dir, speed, baseDamage);
        }
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
            float t = pellets == 1 ? 0.5f : (float)pelletIndex / (pellets - 1);
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

        KinematicProjectile2D proj = go.GetComponent<KinematicProjectile2D>();
        if (proj == null)
        {
            Debug.LogError("[ShotgunBurstAttackModuleSO] Projectile missing KinematicProjectile2D.", weapon);
            return;
        }

        IgnoreOwnerCollisions(go, weapon);

        proj.Initialize(dir, speed, damage, data.targetLayer);

        if (proj is ShotgunSequencePelletProjectile shotgunPellet)
            shotgunPellet.ConfigurePellet(data, null);

        if (data.enableWallBounce)
        {
            IBounceConfigurable bounce = go.GetComponent<IBounceConfigurable>();
            if (bounce != null)
                bounce.ConfigureBounce(data.wallLayer, data.maxBounces, data.bounceSpeedMultiplier);
        }
    }

    private sealed class BurstRunner : MonoBehaviour
    {
        private ShotgunBurstAttackModuleSO module;
        private WeaponBehaviour weapon;
        private ShotgunBurstWeaponDataSO shotgun;
        private bool debugLogs;

        private bool running;
        private int shotsFired;
        private float nextShotTime;
        private float unlockTime;
        private bool waitingUnlock;

        public void Begin(
            ShotgunBurstAttackModuleSO module,
            WeaponBehaviour weapon,
            ShotgunBurstWeaponDataSO shotgun,
            bool debugLogs)
        {
            this.module = module;
            this.weapon = weapon;
            this.shotgun = shotgun;
            this.debugLogs = debugLogs;

            running = true;
            waitingUnlock = false;
            shotsFired = 0;
            nextShotTime = Time.time;

            enabled = true;
        }

        private void Update()
        {
            if (!running || weapon == null || shotgun == null || module == null)
            {
                ForceStop();
                return;
            }

            float now = Time.time;

            if (!waitingUnlock)
            {
                if (now < nextShotTime)
                    return;

                module.FireSingleShot(weapon, shotgun);
                shotsFired++;

                if (debugLogs)
                {
                    Debug.Log($"[ShotgunBurstAttackModuleSO] Burst shot {shotsFired}/{shotgun.burstShotCount}", weapon);
                }

                if (shotsFired >= shotgun.burstShotCount)
                {
                    waitingUnlock = true;
                    unlockTime = now + Mathf.Max(0f, shotgun.postBurstRecoveryDelay);
                    return;
                }

                nextShotTime = now + GetDelayAfterShot(shotsFired);
            }
            else
            {
                if (now >= unlockTime)
                {
                    weapon.UnlockAttack();
                    running = false;
                    enabled = false;
                }
            }
        }

        private float GetDelayAfterShot(int shotIndexJustFired)
        {
            if (shotIndexJustFired == 1)
                return Mathf.Max(0f, shotgun.shot1Delay);

            if (shotIndexJustFired == 2)
                return Mathf.Max(0f, shotgun.shot2Delay);

            return 0f;
        }

        private void OnDisable()
        {
            if (running && weapon != null)
                weapon.UnlockAttack();

            running = false;
            waitingUnlock = false;
        }

        private void ForceStop()
        {
            if (weapon != null)
                weapon.UnlockAttack();

            running = false;
            waitingUnlock = false;
            enabled = false;
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
}