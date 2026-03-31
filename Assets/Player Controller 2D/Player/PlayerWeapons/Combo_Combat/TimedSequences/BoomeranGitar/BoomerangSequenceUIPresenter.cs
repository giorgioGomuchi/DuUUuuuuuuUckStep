using UnityEngine;

public sealed class BoomerangSequenceUIPresenter
{
    private readonly TimedSequenceUIController sequenceUI;

    public BoomerangSequenceUIPresenter(TimedSequenceUIController sequenceUI)
    {
        this.sequenceUI = sequenceUI;
    }

    public void Show(BoomerangSequenceDefinitionSO definition, PlayerReferences refs)
    {
        sequenceUI?.ShowBoomerang(definition, refs);
    }

    public void Hide()
    {
        sequenceUI?.Hide();
    }

    public void Update(
        BoomerangSequenceRuntime runtime,
        BoomerangSequenceDefinitionSO definition,
        SequencePerformanceUISnapshot snapshot)
    {
        if (sequenceUI == null || runtime == null || definition == null)
            return;

        bool useNeutralBar = runtime.Phase == BoomerangSequencePhase.ReturningToReflectZone;

        sequenceUI.SetBoomerangWindowProgress(
            runtime.GetWindowNormalizedTime(),
            runtime.CompletedCycles,
            definition.RequiredSuccessfulCycles,
            GetActiveRuleForCurrentPhase(runtime, definition),
            GetCurrentPhaseLabel(runtime),
            useNeutralBar);

        sequenceUI.SetPerformanceSnapshot(snapshot);
    }

    public void ForceWindowToEnd(
        BoomerangSequenceRuntime runtime,
        BoomerangSequenceDefinitionSO definition,
        TimedSequenceActionRule rule,
        string phaseLabel,
        bool useNeutralBar)
    {
        if (sequenceUI == null || runtime == null || definition == null)
            return;

        sequenceUI.SetBoomerangWindowProgress(
            1f,
            runtime.CompletedCycles,
            definition.RequiredSuccessfulCycles,
            rule,
            phaseLabel,
            useNeutralBar);
    }

    public void FlashJudgement(TimingJudgement judgement)
    {
        sequenceUI?.FlashJudgement(judgement);
    }

    private static TimedSequenceActionRule GetActiveRuleForCurrentPhase(
        BoomerangSequenceRuntime runtime,
        BoomerangSequenceDefinitionSO definition)
    {
        return runtime.Phase switch
        {
            BoomerangSequencePhase.OutboundRecallWindow => definition.RecallRule,
            BoomerangSequencePhase.ReturningToReflectZone => definition.ReflectRule,
            BoomerangSequencePhase.ReflectWindow => definition.ReflectRule,
            _ => definition.RecallRule
        };
    }

    private static string GetCurrentPhaseLabel(BoomerangSequenceRuntime runtime)
    {
        return runtime.Phase switch
        {
            BoomerangSequencePhase.OutboundRecallWindow => "Recall",
            BoomerangSequencePhase.ReturningToReflectZone => "Return",
            BoomerangSequencePhase.ReflectWindow => "Reflect",
            BoomerangSequencePhase.OrbitReward => "Orbit",
            _ => "Sequence"
        };
    }
}