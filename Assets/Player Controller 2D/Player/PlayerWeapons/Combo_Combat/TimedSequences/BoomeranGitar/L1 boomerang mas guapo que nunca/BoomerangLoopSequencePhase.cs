public enum BoomerangLoopSequencePhase
{
    None = 0,
    OutboundRecallWindow = 1,
    ReturningHold = 2,
    CatchDecisionWindow = 3,
    Recovery = 4,
    OrbitReward = 5,
    Completed = 6,
    Failed = 7,
    FailCooldown = 8
}