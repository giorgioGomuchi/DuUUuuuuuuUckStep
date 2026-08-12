using System;
using UnityEngine;

[Serializable]
public class BoomerangLoopSequenceRuntime
{
    [SerializeField] private BoomerangLoopSequencePhase phase = BoomerangLoopSequencePhase.None;
    [SerializeField] private bool isRunning;
    [SerializeField] private bool isInOrbitReward;
    [SerializeField] private bool catchReached;

    [SerializeField] private float windowStartTime;
    [SerializeField] private float windowDuration;
    [SerializeField] private float catchTime;

    public BoomerangLoopSequencePhase Phase => phase;
    public bool IsRunning => isRunning;
    public bool IsInOrbitReward => isInOrbitReward;
    public bool CatchReached => catchReached;
    public float CatchTime => catchTime;

    public void Reset()
    {
        phase = BoomerangLoopSequencePhase.None;
        isRunning = false;
        isInOrbitReward = false;
        catchReached = false;
        windowStartTime = 0f;
        windowDuration = 0f;
        catchTime = 0f;
    }

    public void BeginRecallWindow(float duration)
    {
        isRunning = true;
        isInOrbitReward = false;
        catchReached = false;
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

    public void BeginCatchDecisionWindow(float duration)
    {
        phase = BoomerangLoopSequencePhase.CatchDecisionWindow;
        BeginWindow(duration);
    }


    public void BeginRecovery(float duration)
    {
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

    public void BeginShotRedirectedOutbound(float duration)
    {
        phase = BoomerangLoopSequencePhase.ShotRedirectedOutbound;
        BeginWindow(duration);
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

 

    private void BeginWindow(float duration)
    {
        windowStartTime = Time.time;
        windowDuration = Mathf.Max(0.0001f, duration);
    }

    public void BeginFailCooldown(float duration)
    {
        isRunning = true;
        isInOrbitReward = false;
        phase = BoomerangLoopSequencePhase.FailCooldown;
        BeginWindow(duration);
    }

    public void BeginRecallPendingBeat()
    {
        isRunning = true;
        isInOrbitReward = false;
        catchReached = false;
        phase = BoomerangLoopSequencePhase.RecallPendingBeat;
        windowStartTime = 0f;
        windowDuration = 0f;
    }

    public void BeginDecisionPendingBeat()
    {
        phase = BoomerangLoopSequencePhase.DecisionPendingBeat;
        windowStartTime = 0f;
        windowDuration = 0f;
    }
}