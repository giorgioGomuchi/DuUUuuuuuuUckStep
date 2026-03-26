using UnityEngine;

[System.Serializable]
public class BoomerangSequenceRuntime
{
    [SerializeField] private BoomerangSequencePhase phase = BoomerangSequencePhase.None;
    [SerializeField] private float windowStartTime;
    [SerializeField] private float windowDuration;
    [SerializeField] private int completedCycles;
    [SerializeField] private bool recallDashSucceededThisWindow;
    [SerializeField] private bool reflectDashSucceededThisWindow;
    [SerializeField] private bool isRunning;
    [SerializeField] private bool isInOrbitReward;

    public BoomerangSequencePhase Phase => phase;
    public int CompletedCycles => completedCycles;
    public bool IsRunning => isRunning;
    public bool IsInOrbitReward => isInOrbitReward;
    public bool RecallDashSucceededThisWindow => recallDashSucceededThisWindow;
    public bool ReflectDashSucceededThisWindow => reflectDashSucceededThisWindow;

    public void Reset()
    {
        phase = BoomerangSequencePhase.None;
        windowStartTime = 0f;
        windowDuration = 0f;
        completedCycles = 0;
        recallDashSucceededThisWindow = false;
        reflectDashSucceededThisWindow = false;
        isRunning = false;
        isInOrbitReward = false;
    }

    public void BeginRecallWindow(float duration)
    {
        isRunning = true;
        isInOrbitReward = false;
        phase = BoomerangSequencePhase.OutboundRecallWindow;
        recallDashSucceededThisWindow = false;
        reflectDashSucceededThisWindow = false;
        BeginWindow(duration);
    }

    public void CompleteRecall()
    {
        recallDashSucceededThisWindow = false;
    }

    public void BeginReturnToReflectZone(float duration)
    {
        phase = BoomerangSequencePhase.ReturningToReflectZone;
        BeginWindow(duration);
    }

    public void BeginReflectWindow(float duration)
    {
        phase = BoomerangSequencePhase.ReflectWindow;
        reflectDashSucceededThisWindow = false;
        BeginWindow(duration);
    }

    public void CompleteReflect()
    {
        completedCycles++;
        reflectDashSucceededThisWindow = false;
    }

    public void BeginOrbitReward()
    {
        phase = BoomerangSequencePhase.OrbitReward;
        isInOrbitReward = true;
        windowStartTime = 0f;
        windowDuration = 0f;
        recallDashSucceededThisWindow = false;
        reflectDashSucceededThisWindow = false;
    }

    public void Complete()
    {
        isRunning = false;
        isInOrbitReward = false;
        phase = BoomerangSequencePhase.Completed;
        windowStartTime = 0f;
        windowDuration = 0f;
    }

    public void Fail()
    {
        isRunning = false;
        isInOrbitReward = false;
        phase = BoomerangSequencePhase.Failed;
        windowStartTime = 0f;
        windowDuration = 0f;
    }

    public void RegisterRecallDashSuccess()
    {
        recallDashSucceededThisWindow = true;
    }

    public void RegisterReflectDashSuccess()
    {
        reflectDashSucceededThisWindow = true;
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
}