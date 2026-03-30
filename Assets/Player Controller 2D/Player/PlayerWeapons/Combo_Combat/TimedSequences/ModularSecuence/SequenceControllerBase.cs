using UnityEngine;

public abstract class SequenceControllerBase : MonoBehaviour
{
    [Header("Debug")]
    [SerializeField] protected bool debugLogs = false;

    protected readonly SequenceRuntime runtime = new();

    protected SequenceDefinitionSOBase activeDefinition;
    protected SequencePerformanceTrackerBase performanceTracker;
    protected ISequenceActorAdapter actorAdapter;

    public bool IsSequenceActive => runtime.IsRunning;
    public SequencePhase ActivePhase => runtime.Phase;
    public SequenceDefinitionSOBase ActiveDefinition => activeDefinition;

    protected virtual void Update()
    {
        if (!runtime.IsRunning)
            return;

        runtime.Tick(Time.deltaTime);
        actorAdapter?.TickSequence(Time.deltaTime);

        switch (runtime.Phase)
        {
            case SequencePhase.StepWindow:
                TickStepWindow();
                break;

            case SequencePhase.Completing:
                TickCompleting();
                break;
        }
    }

    protected virtual bool BeginSequenceInternal(
        SequenceDefinitionSOBase definition,
        SequencePerformanceTrackerBase tracker,
        ISequenceActorAdapter adapter)
    {
        if (definition == null || !definition.IsValid())
        {
            Debug.LogWarning("[SequenceControllerBase] Invalid definition.", this);
            return false;
        }

        if (tracker == null || adapter == null || !adapter.IsValid)
        {
            Debug.LogWarning("[SequenceControllerBase] Missing tracker or adapter.", this);
            return false;
        }

        CancelSequenceInternal(false);

        activeDefinition = definition;
        performanceTracker = tracker;
        actorAdapter = adapter;

        performanceTracker.ResetTracker();
        actorAdapter.Bind();
        actorAdapter.OnExternalSequenceFail += HandleExternalFail;
        actorAdapter.OnSequenceStarted(definition);

        runtime.Begin(definition);

        if (debugLogs)
            Debug.Log($"[SequenceControllerBase] Started -> {definition.SequenceId}", this);

        return true;
    }

    protected virtual void CancelSequenceInternal(bool notifyActor = true)
    {
        if (actorAdapter != null)
        {
            actorAdapter.OnExternalSequenceFail -= HandleExternalFail;

            if (notifyActor)
                actorAdapter.OnSequenceCancelled();

            actorAdapter.Unbind();
        }

        runtime.Cancel();
        activeDefinition = null;
        performanceTracker = null;
        actorAdapter = null;
    }

    protected virtual void TickStepWindow()
    {
        if (runtime.IsStepWindowExpired())
        {
            if (activeDefinition != null && activeDefinition.FailOnTimeout)
            {
                FailSequence(SequenceFailReason.Timeout);
                return;
            }

            runtime.OpenCurrentStepWindow();
        }
    }

    protected virtual void TickCompleting()
    {
        if (!runtime.IsCompletionDelayElapsed())
            return;

        CompleteSequenceNow();
    }

    protected virtual void HandleActionResult(SequenceActionResult result)
    {
        if (!result.accepted)
        {
            if (activeDefinition != null && activeDefinition.FailOnWrongAction)
                FailSequence(SequenceFailReason.WrongAction);

            return;
        }

        if (result.perfect)
            performanceTracker?.RegisterPerfect();
        else if (result.good)
            performanceTracker?.RegisterGood();
        else
            performanceTracker?.RegisterSuccess();

        if (result.hits > 0)
        {
            for (int i = 0; i < result.hits; i++)
                performanceTracker?.RegisterHit(0, result.damage);
        }

        if (result.completedStep)
            runtime.MarkCurrentStepCompleted();
    }

    protected virtual void CompleteSequenceNow()
    {
        runtime.CompleteNow();

        SequenceRewardContextBase context =
            performanceTracker != null
                ? performanceTracker.BuildRewardContext(
                    sequenceCompleted: true,
                    completedSteps: runtime.CompletedSteps,
                    attemptedSteps: runtime.CurrentStepIndex + 1)
                : null;

        SequenceRewardResolution resolution = SequenceRewardResolution.None;

        if (activeDefinition != null &&
            activeDefinition.RewardPolicy != null &&
            context != null)
        {
            resolution = activeDefinition.RewardPolicy.Evaluate(context, activeDefinition);
        }

        if (activeDefinition != null &&
            activeDefinition.CompletionReward != null &&
            resolution.shouldApply)
        {
            actorAdapter?.ApplyReward(activeDefinition.CompletionReward, resolution, context);
        }

        actorAdapter?.OnSequenceCompleted();

        if (debugLogs && activeDefinition != null)
            Debug.Log($"[SequenceControllerBase] Completed -> {activeDefinition.SequenceId}", this);

        CleanupAfterFinish();
    }

    protected virtual void FailSequence(SequenceFailReason reason)
    {
        runtime.Fail(reason);
        actorAdapter?.OnSequenceFailed(reason);

        if (debugLogs)
            Debug.Log($"[SequenceControllerBase] Failed -> {reason}", this);

        CleanupAfterFinish();
    }

    protected virtual void CleanupAfterFinish()
    {
        if (actorAdapter != null)
        {
            actorAdapter.OnExternalSequenceFail -= HandleExternalFail;
            actorAdapter.Unbind();
        }

        activeDefinition = null;
        performanceTracker = null;
        actorAdapter = null;
    }

    private void HandleExternalFail(SequenceFailReason reason)
    {
        FailSequence(reason);
    }
}