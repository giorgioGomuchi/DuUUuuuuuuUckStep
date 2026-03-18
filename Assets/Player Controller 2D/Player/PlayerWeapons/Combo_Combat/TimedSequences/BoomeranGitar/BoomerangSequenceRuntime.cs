using UnityEngine;

[System.Serializable]
public sealed class BoomerangSequenceRuntime
{
    public BoomerangSequencePhase Phase { get; private set; } = BoomerangSequencePhase.Inactive;

    public int SuccessfulRecalls { get; private set; }
    public int SuccessfulReflects { get; private set; }
    public int CompletedCycles { get; private set; }

    public bool RecallDashSucceededThisWindow { get; private set; }
    public bool ReflectDashSucceededThisWindow { get; private set; }

    public float WindowStartTime { get; private set; }
    public float WindowEndTime { get; private set; }

    public bool IsRunning { get; private set; }
    public bool IsCompleted => Phase == BoomerangSequencePhase.Completed;
    public bool IsFailed => Phase == BoomerangSequencePhase.Failed;

    public void BeginRecallWindow(float duration)
    {
        IsRunning = true;
        Phase = BoomerangSequencePhase.OutboundRecallWindow;
        RecallDashSucceededThisWindow = false;
        ReflectDashSucceededThisWindow = false;
        OpenWindow(duration);
    }

    public void CompleteRecall()
    {
        SuccessfulRecalls++;
        Phase = BoomerangSequencePhase.ReturningToReflectZone;
        WindowStartTime = 0f;
        WindowEndTime = 0f;
    }

    public void BeginReflectWindow(float duration)
    {
        if (!IsRunning)
            return;

        Phase = BoomerangSequencePhase.ReflectWindow;
        ReflectDashSucceededThisWindow = false;
        OpenWindow(duration);
    }

    public void RegisterRecallDashSuccess()
    {
        RecallDashSucceededThisWindow = true;
    }

    public void RegisterReflectDashSuccess()
    {
        ReflectDashSucceededThisWindow = true;
    }

    public void CompleteReflect()
    {
        SuccessfulReflects++;
        CompletedCycles++;
    }

    public void LoopBackToRecallWindow(float duration)
    {
        if (!IsRunning)
            return;

        Phase = BoomerangSequencePhase.OutboundRecallWindow;
        RecallDashSucceededThisWindow = false;
        ReflectDashSucceededThisWindow = false;
        OpenWindow(duration);
    }

    public void Complete()
    {
        IsRunning = false;
        Phase = BoomerangSequencePhase.Completed;
        WindowStartTime = 0f;
        WindowEndTime = 0f;
    }

    public void Fail()
    {
        IsRunning = false;
        Phase = BoomerangSequencePhase.Failed;
        WindowStartTime = 0f;
        WindowEndTime = 0f;
    }

    public bool IsWindowExpired()
    {
        return IsRunning &&
               WindowEndTime > WindowStartTime &&
               Time.time >= WindowEndTime;
    }

    public float GetWindowNormalizedTime()
    {
        if (WindowEndTime <= WindowStartTime)
            return 0f;

        return Mathf.InverseLerp(WindowStartTime, WindowEndTime, Time.time);
    }

    public void Reset()
    {
        Phase = BoomerangSequencePhase.Inactive;
        SuccessfulRecalls = 0;
        SuccessfulReflects = 0;
        CompletedCycles = 0;
        RecallDashSucceededThisWindow = false;
        ReflectDashSucceededThisWindow = false;
        WindowStartTime = 0f;
        WindowEndTime = 0f;
        IsRunning = false;
    }

    private void OpenWindow(float duration)
    {
        WindowStartTime = Time.time;
        WindowEndTime = Time.time + Mathf.Max(0.01f, duration);
    }
}