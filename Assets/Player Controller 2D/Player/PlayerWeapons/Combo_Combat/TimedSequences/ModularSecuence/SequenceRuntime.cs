using System;
using UnityEngine;

[Serializable]
public sealed class SequenceRuntime
{
    [SerializeField] private SequencePhase phase = SequencePhase.Inactive;
    [SerializeField] private int currentStepIndex = 0;
    [SerializeField] private int completedSteps = 0;
    [SerializeField] private float phaseTime = 0f;
    [SerializeField] private float startupDelay = 0f;
    [SerializeField] private float windowDuration = 0f;
    [SerializeField] private float completionDelay = 0f;
    [SerializeField] private SequenceFailReason failReason = SequenceFailReason.None;

    private SequenceDefinitionSOBase activeDefinition;

    public SequencePhase Phase => phase;
    public int CurrentStepIndex => currentStepIndex;
    public int CompletedSteps => completedSteps;
    public float PhaseTime => phaseTime;
    public SequenceFailReason FailReason => failReason;
    public SequenceDefinitionSOBase ActiveDefinition => activeDefinition;

    public bool IsRunning =>
        phase == SequencePhase.Startup ||
        phase == SequencePhase.StepWindow ||
        phase == SequencePhase.ResolvingStep ||
        phase == SequencePhase.Completing;

    public bool IsFinished =>
        phase == SequencePhase.Completed ||
        phase == SequencePhase.Failed ||
        phase == SequencePhase.Cancelled;

    public bool IsStepWindowOpen => phase == SequencePhase.StepWindow;

    public void Begin(SequenceDefinitionSOBase definition)
    {
        activeDefinition = definition;
        startupDelay = Mathf.Max(0f, definition != null ? definition.StartupDelay : 0f);
        windowDuration = Mathf.Max(0.01f, definition != null ? definition.DecisionWindowDuration : 0.01f);
        completionDelay = Mathf.Max(0f, definition != null ? definition.CompletionDelay : 0f);

        phase = startupDelay > 0f ? SequencePhase.Startup : SequencePhase.StepWindow;
        currentStepIndex = 0;
        completedSteps = 0;
        phaseTime = 0f;
        failReason = SequenceFailReason.None;
    }

    public void Reset()
    {
        activeDefinition = null;
        phase = SequencePhase.Inactive;
        currentStepIndex = 0;
        completedSteps = 0;
        phaseTime = 0f;
        startupDelay = 0f;
        windowDuration = 0f;
        completionDelay = 0f;
        failReason = SequenceFailReason.None;
    }

    public void Tick(float deltaTime)
    {
        if (!IsRunning)
            return;

        phaseTime += Mathf.Max(0f, deltaTime);

        if (phase == SequencePhase.Startup && phaseTime >= startupDelay)
        {
            OpenCurrentStepWindow();
        }
    }

    public void OpenCurrentStepWindow()
    {
        phase = SequencePhase.StepWindow;
        phaseTime = 0f;
    }

    public void BeginResolveStep()
    {
        phase = SequencePhase.ResolvingStep;
        phaseTime = 0f;
    }

    public void MarkCurrentStepCompleted()
    {
        completedSteps++;
        currentStepIndex++;

        if (activeDefinition == null)
        {
            Fail(SequenceFailReason.InvalidDefinition);
            return;
        }

        if (completedSteps >= activeDefinition.RequiredSteps)
        {
            phase = SequencePhase.Completing;
            phaseTime = 0f;
            return;
        }

        phase = SequencePhase.StepWindow;
        phaseTime = 0f;
    }

    public void CompleteNow()
    {
        phase = SequencePhase.Completed;
        phaseTime = 0f;
    }

    public void Fail(SequenceFailReason reason)
    {
        failReason = reason;
        phase = SequencePhase.Failed;
        phaseTime = 0f;
    }

    public void Cancel()
    {
        failReason = SequenceFailReason.CancelledBySystem;
        phase = SequencePhase.Cancelled;
        phaseTime = 0f;
    }

    public bool IsStepWindowExpired()
    {
        return phase == SequencePhase.StepWindow && phaseTime >= windowDuration;
    }

    public bool IsCompletionDelayElapsed()
    {
        if (phase != SequencePhase.Completing)
            return false;

        return phaseTime >= completionDelay;
    }

    public float GetWindowNormalizedTime()
    {
        if (phase != SequencePhase.StepWindow || windowDuration <= 0f)
            return 0f;

        return Mathf.Clamp01(phaseTime / windowDuration);
    }
}