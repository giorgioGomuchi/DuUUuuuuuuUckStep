using UnityEngine;

public enum WeaponSequenceRuntimeState
{
    Inactive = 0,
    Arming = 1,
    WaitingWindow = 2,
    WaitingDashEnd = 3,
    Completing = 4
}

[System.Serializable]
public class WeaponSequenceRuntime
{
    public WeaponSequenceDefinitionSO ActiveDefinition { get; private set; }
    public WeaponSequencePerformance Performance { get; private set; }
    public WeaponSequenceRuntimeState State { get; private set; } = WeaponSequenceRuntimeState.Inactive;

    public float ArmUntilTime { get; private set; }
    public float WindowStartTime { get; private set; }
    public float WindowEndTime { get; private set; }
    public float CompletionAtTime { get; private set; }

    public bool IsActive => ActiveDefinition != null;

    public void Begin(WeaponSequenceDefinitionSO definition)
    {
        ActiveDefinition = definition;
        Performance = new WeaponSequencePerformance();

        State = WeaponSequenceRuntimeState.Arming;
        ArmUntilTime = Time.time + Mathf.Max(0f, definition.StartupDelay);

        WindowStartTime = 0f;
        WindowEndTime = 0f;
        CompletionAtTime = 0f;
    }

    public void OpenDecisionWindow()
    {
        if (ActiveDefinition == null)
            return;

        State = WeaponSequenceRuntimeState.WaitingWindow;
        WindowStartTime = Time.time;
        WindowEndTime = WindowStartTime + ActiveDefinition.DecisionWindowDuration;
    }

    public void EnterWaitingDashEnd()
    {
        State = WeaponSequenceRuntimeState.WaitingDashEnd;
    }

    public void QueueCompletion()
    {
        if (ActiveDefinition == null)
            return;

        State = WeaponSequenceRuntimeState.Completing;
        CompletionAtTime = Time.time + Mathf.Max(0f, ActiveDefinition.CompletionRewardDelay);
    }

    public bool IsArmingComplete()
    {
        return State == WeaponSequenceRuntimeState.Arming && Time.time >= ArmUntilTime;
    }

    public bool IsDecisionWindowExpired()
    {
        return State == WeaponSequenceRuntimeState.WaitingWindow && Time.time >= WindowEndTime;
    }

    public bool IsCompletionReady()
    {
        return State == WeaponSequenceRuntimeState.Completing && Time.time >= CompletionAtTime;
    }

    public float GetWindowNormalizedTime()
    {
        if (State != WeaponSequenceRuntimeState.WaitingWindow)
            return 0f;

        if (WindowEndTime <= WindowStartTime)
            return 1f;

        return Mathf.InverseLerp(WindowStartTime, WindowEndTime, Time.time);
    }

    public void Reset()
    {
        ActiveDefinition = null;
        Performance = null;
        State = WeaponSequenceRuntimeState.Inactive;

        ArmUntilTime = 0f;
        WindowStartTime = 0f;
        WindowEndTime = 0f;
        CompletionAtTime = 0f;
    }
}