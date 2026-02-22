using UnityEngine;

public class EnemySniperBrain : EnemyBrain
{
    private enum SniperState { Idle, Aim, Burst, Cooldown }
    private SniperState state;

    [Header("References")]
    [SerializeField] private EnemyWeapon weapon;
    [SerializeField] private LaserAimModifierSO laserModifier;

    [Header("Debug")]
    [SerializeField] private bool debugLogs;
    [SerializeField] private bool debugGizmos = true;

    private float aimTimer;
    private float burstTimer;
    private float cooldownTimer;
    private int shotsFired;

    protected override void Awake()
    {
        base.Awake();
        if (weapon == null) weapon = GetComponent<EnemyWeapon>();
    }

    private void Start()
    {
        SetState(SniperState.Idle);
    }

    public override void Tick()
    {
        if (controller == null || controller.data == null || controller.player == null || weapon == null)
            return;

        switch (state)
        {
            case SniperState.Idle: UpdateIdle(); break;
            case SniperState.Aim: UpdateAim(); break;
            case SniperState.Burst: UpdateBurst(); break;
            case SniperState.Cooldown: UpdateCooldown(); break;
        }
    }

    private void SetState(SniperState newState)
    {
        state = newState;

        if (debugLogs)
            Debug.Log($"[SniperBrain] -> {state}", this);

        switch (state)
        {
            case SniperState.Idle:
                laserModifier?.DisableLaser(weapon);
                break;

            case SniperState.Aim:
                aimTimer = 0f;
                break;

            case SniperState.Burst:
                burstTimer = 0f;
                shotsFired = 0;
                laserModifier?.DisableLaser(weapon);
                break;

            case SniperState.Cooldown:
                cooldownTimer = controller.data.attackCooldown;
                laserModifier?.DisableLaser(weapon);
                break;
        }
    }

    private void UpdateIdle()
    {
        controller.StopMovement();

        if (controller.PlayerInRange(controller.data.viewDistance))
            SetState(SniperState.Aim);
    }

    private void UpdateAim()
    {
        Vector2 dir = controller.DirectionToPlayer();
        if (dir == Vector2.zero) return;

        // Aim visual del arma
        weapon.Aim(dir);

        // Laser (blink + update positions)
        laserModifier?.UpdateLaser(weapon, controller.player);

        aimTimer += Time.deltaTime;
        if (aimTimer >= controller.data.sniperAimDuration)
            SetState(SniperState.Burst);
    }

    private void UpdateBurst()
    {
        burstTimer -= Time.deltaTime;
        if (burstTimer > 0f) return;

        Vector2 dir = controller.DirectionToPlayer();
        if (dir == Vector2.zero) return;

        // Disparamos 1 proyectil por “step”, pero el SpreadModifier usa shotIndex/shotCount
        weapon.Fire(dir, shotsFired, controller.data.sniperProjectileCount);
        shotsFired++;

        if (shotsFired >= controller.data.sniperProjectileCount)
        {
            SetState(SniperState.Cooldown);
            return;
        }

        burstTimer = controller.data.sniperTimeBetweenShots;
    }

    private void UpdateCooldown()
    {
        cooldownTimer -= Time.deltaTime;
        if (cooldownTimer > 0f) return;

        if (controller.PlayerInRange(controller.data.viewDistance))
            SetState(SniperState.Aim);
        else
            SetState(SniperState.Idle);
    }

    private void OnDrawGizmosSelected()
    {
        if (!debugGizmos || controller == null || controller.data == null) return;

        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, controller.data.viewDistance);
    }
}
