public enum BoomerangLoopSequencePhase
{
    None = 0,
    OutboundRecallWindow = 1,
    ShotRedirectedOutbound = 2,
    ReturningHold = 3,
    CatchDecisionWindow = 4,
    Recovery = 5,
    RecallPendingBeat = 6,
    DecisionPendingBeat = 7,
    OrbitReward = 8,
    Completed = 9,
    Failed = 10,
    FailCooldown = 11
}