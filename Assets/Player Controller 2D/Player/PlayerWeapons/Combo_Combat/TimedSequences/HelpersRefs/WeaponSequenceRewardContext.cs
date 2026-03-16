public class WeaponSequenceRewardContext
{
    public WeaponSequenceDefinitionSO Definition { get; }
    public WeaponSequencePerformance Performance { get; }
    public PlayerReferences PlayerReferences { get; }

    public WeaponSequenceRewardContext(
        WeaponSequenceDefinitionSO definition,
        WeaponSequencePerformance performance,
        PlayerReferences playerReferences)
    {
        Definition = definition;
        Performance = performance;
        PlayerReferences = playerReferences;
    }
}