public enum BoomerangLoopSequencePhase
{
    None = 0,
    OutboundRecallWindow = 1,
    ShotRedirectedOutbound = 2,
    ReturningHold = 3,
    CatchDecisionWindow = 4,
    Recovery = 5,
    OrbitReward = 6,
    Completed = 7,
    Failed = 8,
    FailCooldown = 9
}