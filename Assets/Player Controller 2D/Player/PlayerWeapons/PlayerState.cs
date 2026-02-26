public abstract class PlayerState
{
    protected readonly PlayerStateMachine sm;
    protected readonly PlayerContext ctx;

    protected PlayerState(PlayerStateMachine sm, PlayerContext ctx)
    {
        this.sm = sm;
        this.ctx = ctx;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Tick() { }
    public virtual void FixedTick() { }
}