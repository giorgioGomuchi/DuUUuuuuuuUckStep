using System;
using UnityEngine;

[Serializable]
public class BoomerangLoopSequenceRuntime
{
    [SerializeField] private BoomerangLoopSequencePhase phase = BoomerangLoopSequencePhase.None;
    [SerializeField] private bool isRunning;
    [SerializeField] private bool isInOrbitReward;
    [SerializeField] private bool catchReached;
    [SerializeField] private bool earlyReleaseTriggered;

    [SerializeField] private float windowStartTime;
    [SerializeField] private float windowDuration;
    [SerializeField] private float catchTime;

    public BoomerangLoopSequencePhase Phase => phase;
    public bool IsRunning => isRunning;
    public bool IsInOrbitReward => isInOrbitReward;
    public bool CatchReached => catchReached;
    public bool EarlyReleaseTriggered => earlyReleaseTriggered;
    public float CatchTime => catchTime;

    public void Reset()
    {
        phase = BoomerangLoopSequencePhase.None;
        isRunning = false;
        isInOrbitReward = false;
        catchReached = false;
        earlyReleaseTriggered = false;
        windowStartTime = 0f;
        windowDuration = 0f;
        catchTime = 0f;
    }

    public void BeginRecallWindow(float duration)
    {
        isRunning = true;
        isInOrbitReward = false;
        catchReached = false;
        earlyReleaseTriggered = false;
        phase = BoomerangLoopSequencePhase.OutboundRecallWindow;
        BeginWindow(duration);
    }

    public void BeginReturningHold(float duration)
    {
        phase = BoomerangLoopSequencePhase.ReturningHold;
        BeginWindow(duration);
    }

    public void MarkCatchReached()
    {
        catchReached = true;
        catchTime = Time.time;
    }

    public void BeginCatchReleaseWindow(float duration)
    {
        phase = BoomerangLoopSequencePhase.CatchReleaseWindow;
        BeginWindow(duration);
    }


    public void BeginCatchHold(float duration)
    {
        phase = BoomerangLoopSequencePhase.CatchHold;
        BeginWindow(duration);
    }

    public void BeginReflectWindow(float duration)
    {
        phase = BoomerangLoopSequencePhase.ReflectWindow;
        BeginWindow(duration);
    }

    public void BeginRecovery(float duration)
    {
        earlyReleaseTriggered = true;
        phase = BoomerangLoopSequencePhase.Recovery;
        BeginWindow(duration);
    }

    public void BeginOrbitReward()
    {
        phase = BoomerangLoopSequencePhase.OrbitReward;
        isInOrbitReward = true;
        windowStartTime = 0f;
        windowDuration = 0f;
    }

    public void Complete()
    {
        isRunning = false;
        isInOrbitReward = false;
        phase = BoomerangLoopSequencePhase.Completed;
        windowStartTime = 0f;
        windowDuration = 0f;
    }

    public void Fail()
    {
        isRunning = false;
        isInOrbitReward = false;
        phase = BoomerangLoopSequencePhase.Failed;
        windowStartTime = 0f;
        windowDuration = 0f;
    }

    public bool IsWindowExpired()
    {
        if (windowDuration <= 0f)
            return true;

        return Time.time >= windowStartTime + windowDuration;
    }

    public float GetWindowNormalizedTime()
    {
        if (windowDuration <= 0.0001f)
            return 1f;

        return Mathf.Clamp01((Time.time - windowStartTime) / windowDuration);
    }

    public float GetReleaseNormalizedTimeFromCatch()
    {
        if (windowDuration <= 0.0001f)
            return 1f;

        float elapsedSinceCatch = Mathf.Max(0f, Time.time - catchTime);
        float halfDuration = Mathf.Max(0.0001f, windowDuration * 0.5f);

        return Mathf.Clamp01(0.5f + (elapsedSinceCatch / halfDuration) * 0.5f);
    }

    private void BeginWindow(float duration)
    {
        windowStartTime = Time.time;
        windowDuration = Mathf.Max(0.0001f, duration);
    }
}