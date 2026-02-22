using UnityEngine;

public class EnemyFlyingBazookaBrain : EnemyBrain
{
    private enum State { GroundIdle, Lifting, Flying, Landing, GroundAttack }

    [Header("References")]
    [SerializeField] private EnemyWeapon weapon;
    [SerializeField] private Animator visualAnimator; // Animator del hijo "Visual"

    [Header("Debug")]
    [SerializeField] private bool debugLogs = true;
    [SerializeField] private bool debugGizmos = true;

    [Header("Detection")]
    [SerializeField] private float detectRange = 8f;

    [Header("Flight")]
    [SerializeField] private float flySpeed = 4f;
    [SerializeField] private float hoverHeight = 2.5f;
    [SerializeField] private float arrivalDistance = 0.2f;

    [Header("Attack")]
    [SerializeField] private float attackRange = 7f;
    [SerializeField] private float attackCooldown = 2f;

    // Animator state names (pon EXACTO lo que tengas en el controller)
    private static readonly int WALK_STATE = Animator.StringToHash("eugenia_walk");
    private static readonly int LIFT_STATE = Animator.StringToHash("eugenia_lift");
    private static readonly int FLY_STATE = Animator.StringToHash("eugenia_fly");
    private static readonly int LAND_STATE = Animator.StringToHash("eugenia_land");

    private State state;
    private Vector2 storedTargetPosition;
    private float attackTimer;

    protected override void Awake()
    {
        base.Awake();

        if (weapon == null) weapon = GetComponent<EnemyWeapon>();
        if (visualAnimator == null) visualAnimator = GetComponentInChildren<Animator>();

        if (weapon == null)
        {
            Debug.LogError("[FlyingBazookaBrain] Missing EnemyWeapon.", this);
            enabled = false;
            return;
        }

        if (visualAnimator == null)
        {
            Debug.LogError("[FlyingBazookaBrain] Missing Animator in children (Visual).", this);
            enabled = false;
            return;
        }
    }

    public override void Enter()
    {
        state = State.GroundIdle;
        attackTimer = 0f;

        PlayState(WALK_STATE);

        if (debugLogs)
            Debug.Log("[FlyingBazookaBrain] ENTER -> GroundIdle", this);
    }

    public override void Tick()
    {
        if (controller == null || controller.player == null) return;

        attackTimer -= Time.deltaTime;

        if (debugLogs && Time.frameCount % 60 == 0)
        {
            var st = visualAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[FlyingBazookaBrain] logic={state} animHash={st.shortNameHash} norm={st.normalizedTime:0.00}", this);
        }

        switch (state)
        {
            case State.GroundIdle: UpdateGroundIdle(); break;
            case State.Lifting:      /* espera evento */ break;
            case State.Flying: UpdateFlying(); break;
            case State.Landing:      /* espera evento */ break;
            case State.GroundAttack: UpdateGroundAttack(); break;
        }
    }

    private void UpdateGroundIdle()
    {
        float dist = Vector2.Distance(transform.position, controller.player.position);
        if (dist > detectRange) return;

        storedTargetPosition = controller.player.position;

        if (debugLogs)
            Debug.Log($"[FlyingBazookaBrain] DETECT -> LIFT dist={dist:0.00} target={storedTargetPosition}", this);

        state = State.Lifting;
        PlayState(LIFT_STATE);
    }

    private void UpdateFlying()
    {
        Vector2 flyTarget = storedTargetPosition + Vector2.up * hoverHeight;

        transform.position = Vector2.MoveTowards(
            transform.position,
            flyTarget,
            flySpeed * Time.deltaTime
        );

        if (Vector2.Distance(transform.position, flyTarget) <= arrivalDistance)
        {
            if (debugLogs)
                Debug.Log("[FlyingBazookaBrain] Flying -> Landing (arrived)", this);

            state = State.Landing;
            PlayState(LAND_STATE);
        }
    }

    private void UpdateGroundAttack()
    {
        if (attackTimer > 0f) return;

        float dist = Vector2.Distance(transform.position, controller.player.position);

        if (dist <= attackRange)
        {
            Vector2 dir = controller.DirectionToPlayer();
            if (dir != Vector2.zero)
            {
                // ✅ como el sniper: aim + fire
                weapon.Aim(dir);
                weapon.Fire(dir);

                if (debugLogs)
                    Debug.Log($"[FlyingBazookaBrain] FIRE dir={dir}", this);
            }
        }

        attackTimer = attackCooldown;

        // ✅ vuelve a levantar SIEMPRE
        storedTargetPosition = controller.player.position; // refresca target para la segunda vuelta

        state = State.Lifting;
        PlayState(LIFT_STATE);

        if (debugLogs)
            Debug.Log("[FlyingBazookaBrain] GroundAttack -> Lifting", this);
    }

    // =========================
    // Animation Events (desde el Visual via Relay)
    // =========================

    public void OnLiftFinished()
    {
        if (state != State.Lifting)
        {
            if (debugLogs)
                Debug.LogWarning($"[FlyingBazookaBrain] OnLiftFinished but state={state} (ignored)", this);
            return;
        }

        state = State.Flying;
        PlayState(FLY_STATE);

        if (debugLogs)
            Debug.Log("[FlyingBazookaBrain] EVENT OnLiftFinished -> Flying", this);
    }

    public void OnLandFinished()
    {
        if (state != State.Landing)
        {
            if (debugLogs)
                Debug.LogWarning($"[FlyingBazookaBrain] OnLandFinished but state={state} (ignored)", this);
            return;
        }

        state = State.GroundAttack;
        PlayState(WALK_STATE);

        if (debugLogs)
            Debug.Log("[FlyingBazookaBrain] EVENT OnLandFinished -> GroundAttack", this);
    }

    // =========================
    // Animator helper
    // =========================
    private void PlayState(int stateHash)
    {
        // 0.02f para evitar pops si llamas en frames consecutivos
        visualAnimator.CrossFade(stateHash, 0.02f, 0);
    }

    // =========================
    // Gizmos
    // =========================
    private void OnDrawGizmosSelected()
    {
        if (!debugGizmos) return;

        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);

        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(storedTargetPosition, 0.08f);

        Gizmos.color = Color.cyan;
        Gizmos.DrawLine(transform.position, storedTargetPosition + Vector2.up * hoverHeight);
    }
}
