public class BoomerangSequenceRewardContext
{
    public BoomerangSequenceDefinitionSO Definition { get; }
    public BoomerangSequencePerformance Performance { get; }
    public PlayerReferences PlayerReferences { get; }
    public float ResolvedOrbitDuration { get; }

    public BoomerangSequenceRewardContext(
        BoomerangSequenceDefinitionSO definition,
        BoomerangSequencePerformance performance,
        PlayerReferences playerReferences,
        float resolvedOrbitDuration)
    {
        Definition = definition;
        Performance = performance;
        PlayerReferences = playerReferences;
        ResolvedOrbitDuration = resolvedOrbitDuration;
    }
}