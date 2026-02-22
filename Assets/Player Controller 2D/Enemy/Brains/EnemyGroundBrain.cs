using UnityEngine;

public class EnemyGroundBrain : EnemyBrain
{
    private enum State { Idle, Wander, Chase }

    private State currentState;
    private float timer;
    private Vector2 wanderTarget;

    protected override void Awake()
    {
        base.Awake();
        currentState = State.Idle;
    }

    public override void Tick()
    {
        switch (currentState)
        {
            case State.Idle: UpdateIdle(); break;
            case State.Wander: UpdateWander(); break;
            case State.Chase: UpdateChase(); break;
        }
    }

    private void UpdateIdle()
    {
        controller.StopMovement();

        if (controller.PlayerInRange(controller.data.chaseRange))
        {
            currentState = State.Chase;
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            currentState = State.Wander;
            wanderTarget = (Vector2)controller.transform.position +
                           Random.insideUnitCircle * controller.data.wanderRadius;
        }
    }

    private void UpdateWander()
    {
        if (controller.PlayerInRange(controller.data.chaseRange))
        {
            currentState = State.Chase;
            return;
        }

        controller.MoveTowards(wanderTarget, controller.data.moveSpeed);

        if (Vector2.Distance(controller.transform.position, wanderTarget) < 0.2f)
        {
            timer = controller.data.idleTime;
            currentState = State.Idle;
        }
    }

    private void UpdateChase()
    {
        if (!controller.PlayerInRange(controller.data.chaseRange))
        {
            currentState = State.Idle;
            return;
        }

        float dist = Vector2.Distance(
            controller.transform.position,
            controller.player.position
        );

        if (dist <= controller.data.stopDistance)
        {
            controller.StopMovement();
            // aquí llamas a un EnemyMeleeWeapon
        }
        else
        {
            controller.Move(
                controller.DirectionToPlayer(),
                controller.data.chaseSpeed
            );
        }
    }
}
