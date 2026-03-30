public enum SequenceFailReason
{
    None = 0,
    InvalidDefinition = 1,
    Timeout = 2,
    WrongAction = 3,
    ForbiddenInput = 4,
    ActorRejectedAction = 5,
    MissingReferences = 6,
    ExternalFailure = 7,
    CancelledBySystem = 8
}